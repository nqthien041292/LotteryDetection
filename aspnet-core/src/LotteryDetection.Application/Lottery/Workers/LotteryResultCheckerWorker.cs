using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using LotteryDetection.Lottery.Scraping;
using LotteryDetection.Notifications;

namespace LotteryDetection.Lottery.Workers;

public class LotteryResultCheckerWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IRepository<TicketAnalysis, Guid> _ticketRepository;
    private readonly ILotteryResultProvider _lotteryResultProvider;
    private readonly IAppNotifier _appNotifier;

    public LotteryResultCheckerWorker(
        AbpTimer timer,
        IRepository<TicketAnalysis, Guid> ticketRepository,
        ILotteryResultProvider lotteryResultProvider,
        IAppNotifier appNotifier)
        : base(timer)
    {
        _ticketRepository = ticketRepository;
        _lotteryResultProvider = lotteryResultProvider;
        _appNotifier = appNotifier;

        // Run every 15 minutes
        Timer.Period = 15 * 60 * 1000; 
    }

    [UnitOfWork]
    protected override void DoWork()
    {
        var now = Clock.Now; // Usually UTC
        
        // Convert to Vietnam time (UTC+7)
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // Only run check between 16:00 and 19:00 VN time (XSMN/XSMT finishes ~16:45 to 17:30, XSMB finishes ~18:30)
        // Outside this window, skip to save resources.
        if (vnTime.Hour < 16 || vnTime.Hour >= 19)
        {
            return;
        }

        // Find tickets that don't have IsWinner set but are Succeeded in AI analysis
        var pendingTickets = _ticketRepository.GetAll()
            .Where(t => t.Status == TicketAnalysisStatus.Succeeded && t.IsWinner == null)
            .ToList();

        if (!pendingTickets.Any())
        {
            return;
        }

        // Group by Province and Date to avoid redundant API calls
        var groupedTickets = pendingTickets.GroupBy(t => new { t.Province, t.DrawDate });

        foreach (var group in groupedTickets)
        {
            if (string.IsNullOrEmpty(group.Key.Province) || !group.Key.DrawDate.HasValue)
            {
                continue;
            }

            try
            {
                // Note: GetResultAsync currently throws UserFriendlyException if not found, 
                // but PeriodicBackgroundWorkerBase catches exceptions globally to prevent crash.
                var drawResult = AsyncHelper.RunSync(() => _lotteryResultProvider.GetResultAsync(group.Key.Province, group.Key.DrawDate.Value));

                if (drawResult != null && drawResult.Prizes != null && drawResult.Prizes.Any())
                {
                    foreach (var ticket in group)
                    {
                        LotteryMatcher.Match(ticket, drawResult);

                        // If successfully matched (could be win or lose)
                        if (ticket.IsWinner.HasValue)
                        {
                            AsyncHelper.RunSync(() => _appNotifier.LotteryResultFoundAsync(
                                new Abp.UserIdentifier(ticket.TenantId, ticket.CreatorUserId ?? 0),
                                ticket.Id,
                                ticket.IsWinner.Value,
                                ticket.MatchedPrize,
                                ticket.PrizeAmount
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to check result for {group.Key.Province} on {group.Key.DrawDate}: {ex.Message}");
            }
        }
        
        CurrentUnitOfWork.SaveChanges();
    }
}
