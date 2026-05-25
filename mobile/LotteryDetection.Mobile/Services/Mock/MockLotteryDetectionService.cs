using LotteryDetection.Mobile.Models.Lottery;
using LotteryDetection.Mobile.Services.Interfaces;

namespace LotteryDetection.Mobile.Services.Mock;

public class MockLotteryDetectionService : ILotteryDetectionService
{
    public static readonly MockLotteryDetectionService Instance = new();

    private static readonly string[] Provinces =
    {
        "TP. Hồ Chí Minh",
        "Đồng Nai",
        "Cần Thơ",
        "Bình Dương",
        "Vũng Tàu",
        "Tiền Giang",
        "Kiên Giang"
    };

    private static readonly (string Tier, long Amount)[] Prizes =
    {
        ("giải đặc biệt", 2_000_000_000),
        ("giải nhất", 30_000_000),
        ("giải nhì", 15_000_000),
        ("giải ba", 10_000_000),
        ("giải tư", 3_000_000),
        ("giải năm", 1_000_000),
        ("giải sáu", 400_000),
        ("giải bảy", 200_000),
        ("giải tám", 100_000),
        ("giải khuyến khích", 50_000)
    };

    private static readonly string[] WinningNotes =
    {
        "Vui lòng mang vé số đến đại lý hoặc công ty xổ số trong vòng 30 ngày để nhận thưởng.",
        "Vé số trúng giải. Kiểm tra lại số seri và ngày quay trên vé thật trước khi đi lĩnh thưởng.",
        "Chúc mừng! Lưu ý: vé rách, mờ hoặc hết hạn sẽ không được lĩnh thưởng."
    };

    private static readonly string[] LosingNotes =
    {
        "Không khớp với kết quả mở thưởng của đài tương ứng trong ngày quay.",
        "Dãy số không nằm trong danh sách trúng giải. Hãy thử kỳ quay tiếp theo.",
        "Chúc bạn may mắn lần sau!"
    };

    private readonly Random random = new();

    public async Task<List<LotteryTicketResult>> AnalyzeAsync(string imagePath, CancellationToken ct)
    {
        // Simulate AI processing latency.
        await Task.Delay(TimeSpan.FromMilliseconds(1100 + random.Next(400)), ct);

        var count = random.Next(1, 3); // 1 to 2 tickets
        var results = new List<LotteryTicketResult>();

        for (int i = 0; i < count; i++)
        {
            var province = Provinces[random.Next(Provinces.Length)];
            var ticketNumber = random.Next(0, 1_000_000).ToString("D6");
            var drawDate = DateTime.Today.AddDays(-random.Next(0, 3));
            var confidence = 0.78 + random.NextDouble() * 0.19;

            var isWinner = random.NextDouble() < 0.35;
            string? prize = null;
            long? amount = null;
            string note;

            if (isWinner)
            {
                var weighted = random.Next(100);
                var tierIndex = weighted switch
                {
                    < 2 => 0, // ĐB
                    < 6 => 1, // nhất
                    < 12 => 2, // nhì
                    < 22 => 3, // ba
                    < 35 => 4, // tư
                    < 50 => 5, // năm
                    < 65 => 6, // sáu
                    < 78 => 7, // bảy
                    < 90 => 8, // tám
                    _ => 9  // KK
                };
                var pick = Prizes[tierIndex];
                prize = pick.Tier;
                amount = pick.Amount;
                note = WinningNotes[random.Next(WinningNotes.Length)];
            }
            else
            {
                note = LosingNotes[random.Next(LosingNotes.Length)];
            }

            results.Add(new LotteryTicketResult
            {
                Province = province,
                DrawDate = drawDate,
                TicketNumber = ticketNumber,
                IsWinner = isWinner,
                MatchedPrize = prize,
                PrizeAmount = amount,
                Confidence = Math.Round(confidence, 2),
                Notes = note,
                ImagePath = imagePath,
                AnalyzedAt = DateTime.Now
            });
        }

        return results;
    }
}
