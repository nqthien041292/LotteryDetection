namespace LotteryDetection.Mobile.Models.Lottery;

public class LotteryHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CapturedAt { get; set; }
    public string Province { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public DateTime DrawDate { get; set; }
    public bool IsWinner { get; set; }
    public string? MatchedPrize { get; set; }
    public long? PrizeAmount { get; set; }

    public string CapturedDisplay => CapturedAt.Date == DateTime.Today
        ? $"Hôm nay · {CapturedAt:HH:mm}"
        : CapturedAt.Date == DateTime.Today.AddDays(-1)
            ? $"Hôm qua · {CapturedAt:HH:mm}"
            : CapturedAt.ToString("dd/MM · HH:mm");

    public string DrawDateDisplay => DrawDate.ToString("dd/MM/yyyy");
    public string PrizeAmountDisplay => PrizeAmount.HasValue ? $"{PrizeAmount.Value:N0} đ" : "—";

    public string StatusLabel => IsWinner ? (MatchedPrize ?? "Đã trúng") : "Chưa trúng";
    public string StatusBackground => IsWinner ? "#F5C842" : "#F2F4F0";
    public string StatusTextColor => IsWinner ? "#173D2A" : "#66736A";

    public string ProvinceAbbreviation
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Province)) return "XS";
            string clean = Province.ToLower().Trim();

            if (clean.Contains("miền bắc") || clean.Contains("hà nội") || clean.Contains("mien bac") || clean.Contains("ha noi")) return "MB";
            if (clean.Contains("hồ chí minh") || clean.Contains("hcm") || clean.Contains("tp. hcm") || clean.Contains("tp.hcm")) return "HC";
            if (clean.Contains("bình dương") || clean.Contains("binh duong")) return "BD";
            if (clean.Contains("bình phước") || clean.Contains("binh phuoc")) return "BP";
            if (clean.Contains("long an")) return "LA";
            if (clean.Contains("đà nẵng") || clean.Contains("da nang")) return "DN";
            if (clean.Contains("hậu giang") || clean.Contains("hau giang")) return "HG";
            if (clean.Contains("gia lai")) return "GL";
            if (clean.Contains("ninh thuận") || clean.Contains("ninh thuan")) return "NT";
            if (clean.Contains("quảng ngãi") || clean.Contains("quang ngai")) return "QNg";
            if (clean.Contains("quảng bình") || clean.Contains("quang binh")) return "QB";
            if (clean.Contains("quảng trị") || clean.Contains("quang tri")) return "QT";
            if (clean.Contains("quảng nam") || clean.Contains("quang nam")) return "QNa";
            if (clean.Contains("tây ninh") || clean.Contains("tay ninh")) return "TN";
            if (clean.Contains("vĩnh long") || clean.Contains("vinh long")) return "VL";
            if (clean.Contains("trà vinh") || clean.Contains("tra vinh")) return "TV";
            if (clean.Contains("vũng tàu") || clean.Contains("vung tau")) return "VT";
            if (clean.Contains("bến tre") || clean.Contains("ben tre")) return "BT";
            if (clean.Contains("bạc liêu") || clean.Contains("bac lieu")) return "BL";
            if (clean.Contains("đồng nai") || clean.Contains("dong nai")) return "ĐN";
            if (clean.Contains("cần thơ") || clean.Contains("can tho")) return "CT";
            if (clean.Contains("sóc trăng") || clean.Contains("soc trang")) return "ST";
            if (clean.Contains("an giang")) return "AG";
            if (clean.Contains("bình thuận") || clean.Contains("binh thuan")) return "BTh";
            if (clean.Contains("tiền giang") || clean.Contains("tien giang")) return "TG";
            if (clean.Contains("kiên giang") || clean.Contains("kien giang")) return "KG";
            if (clean.Contains("đà lạt") || clean.Contains("da lat") || clean.Contains("lâm đồng") || clean.Contains("lam dong")) return "DL";
            if (clean.Contains("đồng tháp") || clean.Contains("dong thap")) return "ĐT";
            if (clean.Contains("cà mau") || clean.Contains("ca mau")) return "CM";
            if (clean.Contains("đắk lắk") || clean.Contains("dak lak")) return "DLk";
            if (clean.Contains("đắk nông") || clean.Contains("dak nong")) return "DNg";
            if (clean.Contains("khánh hòa") || clean.Contains("khanh hoa")) return "KH";
            if (clean.Contains("phú yên") || clean.Contains("phu yen")) return "PY";
            if (clean.Contains("thừa thiên huế") || clean.Contains("huế") || clean.Contains("hue")) return "TTH";
            if (clean.Contains("kon tum")) return "KT";

            // Fallback to first letters
            var words = Province.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return (words[0][0].ToString() + words[1][0].ToString()).ToUpper();
            }
            return Province.Length >= 2 ? Province.Substring(0, 2).ToUpper() : "XS";
        }
    }

    public string ProvinceLogoColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Province)) return "#475569"; // slate-600
            string clean = Province.ToLower().Trim();

            // Distinct elegant brand colors for key lottery companies
            if (clean.Contains("miền bắc") || clean.Contains("hà nội") || clean.Contains("mien bac") || clean.Contains("ha noi")) return "#DC2626"; // Vibrant Red
            if (clean.Contains("hồ chí minh") || clean.Contains("hcm") || clean.Contains("tp. hcm") || clean.Contains("tp.hcm")) return "#059669"; // Emerald Green
            if (clean.Contains("bình dương") || clean.Contains("binh duong")) return "#2563EB"; // Royal Blue
            if (clean.Contains("bình phước") || clean.Contains("binh phuoc")) return "#4F46E5"; // Indigo
            if (clean.Contains("long an")) return "#D97706"; // Amber Orange
            if (clean.Contains("đà nẵng") || clean.Contains("da nang")) return "#7C3AED"; // Purple
            if (clean.Contains("hậu giang") || clean.Contains("hau giang")) return "#0891B2"; // Cyan
            if (clean.Contains("gia lai")) return "#0D9488"; // Teal
            if (clean.Contains("ninh thuận") || clean.Contains("ninh thuan")) return "#DB2777"; // Pink
            if (clean.Contains("quảng ngãi") || clean.Contains("quang ngai")) return "#EA580C"; // Orange-Red
            if (clean.Contains("quảng bình") || clean.Contains("quang binh")) return "#15803D"; // Green
            if (clean.Contains("quảng trị") || clean.Contains("quang tri")) return "#84CC16"; // Lime
            if (clean.Contains("tây ninh") || clean.Contains("tay ninh")) return "#0284C7"; // Sky Blue
            if (clean.Contains("vĩnh long") || clean.Contains("vinh long")) return "#4F46E5"; // Indigo
            if (clean.Contains("trà vinh") || clean.Contains("tra vinh")) return "#9333EA"; // Purple

            // Region based fallbacks
            if (clean.Contains("miền trung") || clean.Contains("mt")) return "#7C3AED";
            if (clean.Contains("miền nam") || clean.Contains("mn")) return "#059669";

            // Hash code based stable color generation to ensure variety
            int hash = Province.GetHashCode();
            string[] colors = { "#059669", "#2563EB", "#7C3AED", "#D97706", "#DB2777", "#0891B2", "#0D9488", "#EA580C", "#4F46E5" };
            return colors[Math.Abs(hash) % colors.Length];
        }
    }

    public string TicketBadgeBackground => IsWinner ? "#F5C842" : ProvinceLogoColor;
    public string TicketBadgeTextColor => IsWinner ? "#173D2A" : "White";

    // Winner-specific visual emphasis used in the redesigned history list.
    public string CardBackground => IsWinner ? "#173D2A" : "White";
    public string CardStrokeColor => IsWinner ? "#173D2A" : "#DAE5D6";
    public string PrimaryTextColor => IsWinner ? "White" : "#142116";
    public string SecondaryTextColor => IsWinner ? "#CFE6D5" : "#38443B";
    public string MutedTextColor => IsWinner ? "#A9D7B6" : "#8A958D";
    public string TicketNumberColor => IsWinner ? "#F5C842" : "#142116";
    public string PrizeAmountColor => IsWinner ? "#F5C842" : "#173D2A";
    public double PrizeAmountFontSize => IsWinner ? 18 : 12;
    public bool ShowTrophy => IsWinner;
    public string TicketBadgeIcon => IsWinner ? "🏆" : ProvinceAbbreviation;
    public double TicketBadgeFontSize => IsWinner ? 22 : 12.5;

    public string ProvinceLogoImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Province)) return "logo_xs_mn.png"; // Southern default
            string clean = Province.ToLower().Trim();

            // Specific customized logos
            if (clean.Contains("hồ chí minh") || clean.Contains("hcm") || clean.Contains("tp. hcm") || clean.Contains("tp.hcm")) 
                return "logo_xs_hcm.png";

            if (clean.Contains("miền bắc") || clean.Contains("hà nội") || clean.Contains("mien bac") || clean.Contains("ha noi")) 
                return "logo_xs_mb.png";

            // Central Vietnam Region provinces
            if (clean.Contains("đà nẵng") || clean.Contains("da nang") || clean.Contains("gia lai") || clean.Contains("ninh thuận") || 
                clean.Contains("ninh thuan") || clean.Contains("quảng ngãi") || clean.Contains("quang ngai") || clean.Contains("quảng bình") || 
                clean.Contains("quang binh") || clean.Contains("quảng trị") || clean.Contains("quang tri") || clean.Contains("quảng nam") || 
                clean.Contains("quang nam") || clean.Contains("đắk lắk") || clean.Contains("dak lak") || clean.Contains("đắk nông") || 
                clean.Contains("dak nong") || clean.Contains("khánh hòa") || clean.Contains("khanh hoa") || clean.Contains("phú yên") || 
                clean.Contains("phu yen") || clean.Contains("thừa thiên huế") || clean.Contains("huế") || clean.Contains("hue") || 
                clean.Contains("kon tum") || clean.Contains("miền trung") || clean.Contains("mt"))
                return "logo_xs_mt.png";

            // Southern Vietnam Region default
            return "logo_xs_mn.png";
        }
    }
}
