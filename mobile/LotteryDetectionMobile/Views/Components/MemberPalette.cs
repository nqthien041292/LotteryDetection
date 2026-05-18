namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Resolves the per-member colour triple (bg / text / dot) defined in the active theme dictionary.
///     Falls back to slate if the member id is unknown.
/// </summary>
internal static class MemberPalette
{
    public enum Slot
    {
        Bg,
        Text,
        Dot
    }

    public static Color Resolve(string memberId, Slot slot)
    {
        var member = Normalize(memberId);
        var slotName = slot switch
        {
            Slot.Bg => "Bg",
            Slot.Text => "Text",
            _ => "Dot"
        };
        var fallback = slot switch
        {
            Slot.Bg => Color.FromArgb("#E5E7EF"),
            Slot.Text => Color.FromArgb("#334155"),
            _ => Color.FromArgb("#64748B")
        };
        return ResourceLookup.Color($"FamilyMember{member}{slotName}Light", $"FamilyMember{member}{slotName}Dark", fallback);
    }

    private static readonly string[] PaletteNames = { "Alex", "Sam", "Jordan", "Riley" };

    private static string Normalize(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return "Home";

        return memberId.ToLowerInvariant() switch
        {
            "alex" or "dad" => "Alex",
            "sam" or "mom" => "Sam",
            "jordan" or "teen" => "Jordan",
            "riley" or "kid" => "Riley",
            "home" or "system" => "Home",
            // Unknown ID (e.g. GUID): deterministic hash → one of the palette slots
            _ => PaletteNames[Math.Abs(memberId.GetHashCode()) % PaletteNames.Length]
        };
    }
}
