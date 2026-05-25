using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LotteryDetection.Lottery;

/// <summary>
///     Maps free-form province strings returned by Gemini (e.g. "Mien Bac", "TPHCM",
///     "TP. HCM", "ho chi minh") to the canonical Vietnamese display name used by the
///     mobile UI. Unknown inputs are returned untouched after trimming.
/// </summary>
internal static class VietnameseProvinceNormalizer
{
    private static readonly Dictionary<string, string> CanonicalByKey = BuildIndex();

    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        var key = ToKey(raw);
        return CanonicalByKey.TryGetValue(key, out var canonical) ? canonical : raw.Trim();
    }

    private static Dictionary<string, string> BuildIndex()
    {
        // Canonical name → list of aliases (case/diacritics/punctuation insensitive).
        var aliases = new Dictionary<string, string[]>
        {
            ["Miền Bắc"] = new[] { "mien bac", "mb", "xsmb", "northern", "ha noi" },
            ["Miền Trung"] = new[] { "mien trung", "xsmt", "central" },
            ["Miền Nam"] = new[] { "mien nam", "xsmn", "southern" },

            ["TP. HCM"] = new[]
            {
                "tp ho chi minh", "thanh pho ho chi minh", "ho chi minh", "hcm", "tphcm",
                "tp hcm", "sai gon", "saigon", "xshcm", "tp. ho chi minh"
            },

            ["An Giang"] = new[] { "an giang", "xsag" },
            ["Vũng Tàu"] = new[] { "ba ria vung tau", "vung tau", "xsvt", "brvt", "ba ria - vung tau" },
            ["Bạc Liêu"] = new[] { "bac lieu", "xsbl" },
            ["Bắc Giang"] = new[] { "bac giang", "xsbg" },
            ["Bắc Kạn"] = new[] { "bac kan" },
            ["Bắc Ninh"] = new[] { "bac ninh", "xsbn" },
            ["Bến Tre"] = new[] { "ben tre", "xsbt" },
            ["Bình Định"] = new[] { "binh dinh", "xsbdi" },
            ["Bình Dương"] = new[] { "binh duong", "xsbd" },
            ["Bình Phước"] = new[] { "binh phuoc", "xsbp" },
            ["Bình Thuận"] = new[] { "binh thuan", "xsbth" },
            ["Cà Mau"] = new[] { "ca mau", "xscm" },
            ["Cao Bằng"] = new[] { "cao bang" },
            ["Cần Thơ"] = new[] { "can tho", "xsct" },
            ["Đà Nẵng"] = new[] { "da nang", "xsdng" },
            ["Đắk Lắk"] = new[] { "dak lak", "daklak", "xsdlk" },
            ["Đắk Nông"] = new[] { "dak nong", "xsdno" },
            ["Điện Biên"] = new[] { "dien bien" },
            ["Đồng Nai"] = new[] { "dong nai", "xsdn" },
            ["Đồng Tháp"] = new[] { "dong thap", "xsdt" },
            ["Gia Lai"] = new[] { "gia lai", "xsgl" },
            ["Hà Giang"] = new[] { "ha giang" },
            ["Hà Nam"] = new[] { "ha nam" },
            ["Hà Nội"] = new[] { "ha noi", "xshn", "hanoi" },
            ["Hà Tĩnh"] = new[] { "ha tinh" },
            ["Hải Dương"] = new[] { "hai duong" },
            ["Hải Phòng"] = new[] { "hai phong", "xshp" },
            ["Hậu Giang"] = new[] { "hau giang", "xshg" },
            ["Hòa Bình"] = new[] { "hoa binh" },
            ["Hưng Yên"] = new[] { "hung yen" },
            ["Khánh Hòa"] = new[] { "khanh hoa", "xskh" },
            ["Kiên Giang"] = new[] { "kien giang", "xskg" },
            ["Kon Tum"] = new[] { "kon tum", "xskt" },
            ["Lai Châu"] = new[] { "lai chau" },
            ["Đà Lạt"] = new[] { "lam dong", "xsld", "da lat", "lam dong" },
            ["Lạng Sơn"] = new[] { "lang son" },
            ["Lào Cai"] = new[] { "lao cai" },
            ["Long An"] = new[] { "long an", "xsla" },
            ["Nam Định"] = new[] { "nam dinh", "xsnd" },
            ["Nghệ An"] = new[] { "nghe an" },
            ["Ninh Bình"] = new[] { "ninh binh" },
            ["Ninh Thuận"] = new[] { "ninh thuan", "xsnt" },
            ["Phú Thọ"] = new[] { "phu tho" },
            ["Phú Yên"] = new[] { "phu yen", "xspy" },
            ["Quảng Bình"] = new[] { "quang binh", "xsqb" },
            ["Quảng Nam"] = new[] { "quang nam", "xsqnm" },
            ["Quảng Ngãi"] = new[] { "quang ngai", "xsqng" },
            ["Quảng Ninh"] = new[] { "quang ninh", "xsqn" },
            ["Quảng Trị"] = new[] { "quang tri", "xsqt" },
            ["Sóc Trăng"] = new[] { "soc trang", "xsst" },
            ["Sơn La"] = new[] { "son la" },
            ["Tây Ninh"] = new[] { "tay ninh", "xstn" },
            ["Thái Bình"] = new[] { "thai binh", "xstb" },
            ["Thái Nguyên"] = new[] { "thai nguyen" },
            ["Thanh Hóa"] = new[] { "thanh hoa" },
            ["Thừa Thiên Huế"] = new[] { "thua thien hue", "hue", "tt hue", "xstth" },
            ["Tiền Giang"] = new[] { "tien giang", "xstg" },
            ["Trà Vinh"] = new[] { "tra vinh", "xstv" },
            ["Tuyên Quang"] = new[] { "tuyen quang" },
            ["Vĩnh Long"] = new[] { "vinh long", "xsvl" },
            ["Vĩnh Phúc"] = new[] { "vinh phuc" },
            ["Yên Bái"] = new[] { "yen bai" }
        };

        var index = new Dictionary<string, string>(System.StringComparer.Ordinal);
        foreach (var (canonical, list) in aliases)
        {
            index[ToKey(canonical)] = canonical;
            foreach (var alias in list) index[ToKey(alias)] = canonical;
        }
        return index;
    }

    private static string ToKey(string input)
    {
        var stripped = StripDiacritics(input).ToLowerInvariant();
        return new string(stripped.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray())
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(new StringBuilder(), (sb, w) => sb.Length == 0 ? sb.Append(w) : sb.Append(' ').Append(w))
            .ToString();
    }

    private static string StripDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        return sb.ToString()
            .Replace('đ', 'd').Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormC);
    }
}
