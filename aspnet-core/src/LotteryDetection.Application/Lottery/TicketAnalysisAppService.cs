using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using LotteryDetection.Authorization;
using LotteryDetection.Lottery.Dto;
using LotteryDetection.Lottery.Gcp;
using LotteryDetection.Storage;
using Microsoft.EntityFrameworkCore;

namespace LotteryDetection.Lottery;

[AbpAuthorize]
public class TicketAnalysisAppService : LotteryDetectionAppServiceBase, ITicketAnalysisAppService
{
    private const long MaxImageBytes = 8 * 1024 * 1024;

    private readonly IRepository<TicketAnalysis, Guid> _repository;
    private readonly IBinaryObjectManager _binaryObjectManager;
    private readonly IVertexAITicketAnalyzer _analyzer;
    private readonly ILotteryResultProvider _lotteryResultProvider;
    private readonly Notifications.IAppNotifier _appNotifier;

    public TicketAnalysisAppService(
        IRepository<TicketAnalysis, Guid> repository,
        IBinaryObjectManager binaryObjectManager,
        IVertexAITicketAnalyzer analyzer,
        ILotteryResultProvider lotteryResultProvider,
        Notifications.IAppNotifier appNotifier)
    {
        _repository = repository;
        _binaryObjectManager = binaryObjectManager;
        _analyzer = analyzer;
        _lotteryResultProvider = lotteryResultProvider;
        _appNotifier = appNotifier;
    }

    [AbpAllowAnonymous]
    public async Task CheckPendingResultsAsync()
    {
        var pendingTickets = await _repository.GetAll()
            .Where(t => t.Status == TicketAnalysisStatus.Succeeded && t.IsWinner == null)
            .ToListAsync();

        if (!pendingTickets.Any())
        {
            return;
        }

        var groupedTickets = pendingTickets.GroupBy(t => new { t.Province, t.DrawDate });

        foreach (var group in groupedTickets)
        {
            if (string.IsNullOrEmpty(group.Key.Province) || !group.Key.DrawDate.HasValue)
            {
                continue;
            }

            try
            {
                var drawResult = await _lotteryResultProvider.GetResultAsync(group.Key.Province, group.Key.DrawDate.Value);

                if (drawResult != null && drawResult.Prizes != null && drawResult.Prizes.Any())
                {
                    foreach (var ticket in group)
                    {
                        LotteryMatcher.Match(ticket, drawResult);

                        if (ticket.IsWinner.HasValue && ticket.CreatorUserId.HasValue)
                        {
                            await _appNotifier.LotteryResultFoundAsync(
                                new Abp.UserIdentifier(ticket.TenantId, ticket.CreatorUserId.Value),
                                ticket.Id,
                                ticket.IsWinner.Value,
                                ticket.MatchedPrize,
                                ticket.PrizeAmount
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to check result for {group.Key.Province} on {group.Key.DrawDate}: {ex.Message}");
            }
        }
        
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private static readonly string[] ActiveProvinces = new[]
    {
        // Miền Bắc
        "Miền Bắc",
        
        // Miền Trung
        "Thừa Thiên Huế", "Phú Yên", "Đắk Lắk", "Quảng Nam", "Đà Nẵng", 
        "Khánh Hòa", "Bình Định", "Quảng Trị", "Quảng Bình", "Gia Lai", 
        "Ninh Thuận", "Đắk Nông", "Quảng Ngãi", "Kon Tum",
        
        // Miền Nam
        "TP. HCM", "Đồng Tháp", "Cà Mau", "Bến Tre", "Vũng Tàu", 
        "Bạc Liêu", "Đồng Nai", "Cần Thơ", "Sóc Trăng", "Tây Ninh", 
        "An Giang", "Bình Thuận", "Vĩnh Long", "Bình Dương", "Trà Vinh", 
        "Long An", "Hậu Giang", "Bình Phước", "Tiền Giang", "Kiên Giang", 
        "Đà Lạt"
    };

    [AbpAllowAnonymous]
    public async Task TriggerScrapeLast7DaysAsync()
    {
        var vnTimeNow = DateTime.UtcNow.AddHours(7);
        var dates = Enumerable.Range(0, 7)
            .Select(offset => vnTimeNow.AddDays(-offset).Date)
            .ToList();

        foreach (var date in dates)
        {
            var dayOfWeek = date.DayOfWeek;
            foreach (var province in ActiveProvinces)
            {
                if (Scraping.MinhNgocResultProvider.IsProvinceActiveOnDayOfWeek(province, dayOfWeek))
                {
                    try
                    {
                        // Gọi GetResultAsync với allowScrape: true để cào dữ liệu mới
                        await _lotteryResultProvider.GetResultAsync(province, date, allowScrape: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to trigger scrape for {province} on {date:yyyy-MM-dd}: {ex.Message}");
                    }
                }
            }
        }

        // Sau khi cào, cập nhật lại trạng thái các vé số đang chờ kết quả của đài vừa được cào
        await CheckPendingResultsAsync();
    }

    [AbpAllowAnonymous]
    public async Task TriggerScrapeLast30DaysAsync()
    {
        var vnTimeNow = DateTime.UtcNow.AddHours(7);
        var dates = Enumerable.Range(0, 30)
            .Select(offset => vnTimeNow.AddDays(-offset).Date)
            .ToList();

        foreach (var date in dates)
        {
            var dayOfWeek = date.DayOfWeek;
            foreach (var province in ActiveProvinces)
            {
                if (Scraping.MinhNgocResultProvider.IsProvinceActiveOnDayOfWeek(province, dayOfWeek))
                {
                    try
                    {
                        // Gọi GetResultAsync với allowScrape: true để cào dữ liệu mới
                        await _lotteryResultProvider.GetResultAsync(province, date, allowScrape: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to trigger scrape for {province} on {date:yyyy-MM-dd}: {ex.Message}");
                    }
                }
            }
        }

        // Sau khi cào, cập nhật lại trạng thái các vé số đang chờ kết quả của đài vừa được cào
        await CheckPendingResultsAsync();
    }


    public async Task<TicketAnalysisDto> AnalyzeAsync(AnalyzeTicketInput input)
    {
        if (input?.ImageBytes == null || input.ImageBytes.Length == 0)
        {
            throw new UserFriendlyException("Ảnh không hợp lệ.");
        }

        if (input.ImageBytes.Length > MaxImageBytes)
        {
            throw new UserFriendlyException($"Ảnh vượt quá {MaxImageBytes / (1024 * 1024)}MB.");
        }

        if (input.ContentType.IsNullOrWhiteSpace() ||
            !input.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new UserFriendlyException("Định dạng file không phải ảnh.");
        }

        var binaryObject = new BinaryObject(AbpSession.TenantId, input.ImageBytes,
            $"LotteryTicket:{input.FileName ?? "scan"}");
        await _binaryObjectManager.SaveAsync(binaryObject);

        var entity = new TicketAnalysis
        {
            Id = Guid.NewGuid(),
            TenantId = AbpSession.TenantId,
            ImageBinaryObjectId = binaryObject.Id,
            Status = TicketAnalysisStatus.Pending
        };

        try
        {
            var result = await _analyzer.AnalyzeAsync(input.ImageBytes, input.ContentType);

            entity.Province = Truncate(VietnameseProvinceNormalizer.Normalize(result.Province), TicketAnalysis.ProvinceMaxLength);
            entity.DrawDate = result.DrawDate;
            entity.TicketNumber = Truncate(result.TicketNumber, TicketAnalysis.TicketNumberMaxLength);
            entity.DrawType = Truncate(result.DrawType, TicketAnalysis.DrawTypeMaxLength);
            entity.Confidence = result.Confidence;
            entity.Notes = Truncate(result.Notes, TicketAnalysis.NotesMaxLength);
            entity.RawModelResponse = Truncate(result.RawJson, TicketAnalysis.RawModelResponseMaxLength);
            entity.Status = TicketAnalysisStatus.Succeeded;

            if (!string.IsNullOrEmpty(entity.Province) && entity.DrawDate.HasValue && !string.IsNullOrEmpty(entity.TicketNumber))
            {
                var drawResult = await _lotteryResultProvider.GetResultAsync(entity.Province, entity.DrawDate.Value, allowScrape: false);
                if (drawResult != null)
                {
                    LotteryMatcher.Match(entity, drawResult);
                }
                else
                {
                    entity.IsWinner = null;
                    entity.Notes = Truncate("Vé số chưa có kết quả. Chúng tôi sẽ thông báo sớm nhất", TicketAnalysis.NotesMaxLength);
                }
            }
        }
        catch (UserFriendlyException ex)
        {
            entity.Status = TicketAnalysisStatus.Failed;
            entity.ErrorMessage = Truncate(ex.Message, TicketAnalysis.ErrorMessageMaxLength);
            await _repository.InsertAsync(entity);
            throw;
        }

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<TicketAnalysisDto> GetAsync(EntityDto<Guid> input)
    {
        var entity = await _repository.FirstOrDefaultAsync(input.Id);
        if (entity == null) throw new UserFriendlyException("Không tìm thấy bản ghi.");
        return MapToDto(entity);
    }

    public async Task<PagedResultDto<TicketAnalysisDto>> GetHistoryAsync(PagedResultRequestDto input)
    {
        var query = _repository.GetAll()
            .Where(t => t.CreatorUserId == AbpSession.UserId)
            .OrderByDescending(t => t.CreationTime);

        var total = await query.CountAsync();
        var items = await query
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TicketAnalysisDto>(total, items.Select(MapToDto).ToList());
    }

    private static TicketAnalysisDto MapToDto(TicketAnalysis e) => new()
    {
        Id = e.Id,
        ImageBinaryObjectId = e.ImageBinaryObjectId,
        Province = e.Province,
        DrawDate = e.DrawDate,
        TicketNumber = e.TicketNumber,
        DrawType = e.DrawType,
        Confidence = e.Confidence,
        IsWinner = e.IsWinner,
        MatchedPrize = e.MatchedPrize,
        PrizeAmount = e.PrizeAmount,
        Notes = e.Notes,
        Status = e.Status,
        ErrorMessage = e.ErrorMessage,
        CreationTime = e.CreationTime
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
}
