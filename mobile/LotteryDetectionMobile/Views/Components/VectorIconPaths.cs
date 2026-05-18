using System.Globalization;

namespace LotteryDetectionMobile.Views.Components;

internal enum IconRender
{
    Stroke,
    Fill
}

internal sealed record IconLayer(string Data, IconRender Render, double Opacity = 1.0);

/// <summary>
///     Design-bundle SVG icon path data (viewBox 0 0 22 22), ported verbatim from
///     docs/familyai2 (brand.jsx / screens-home.jsx). Stroke icons use a 1.7 width
///     expressed in the 22-unit space so the visual weight scales with the icon.
/// </summary>
internal static class VectorIconPaths
{
    public const double ViewBox = 22.0;
    public const double StrokeWidth = 1.7;

    public static IReadOnlyList<IconLayer> For(string glyph) => glyph switch
    {
        "bell" => new[]
        {
            new IconLayer("M6 8a5 5 0 0 1 10 0c0 6 2.5 7.5 2.5 7.5h-15S6 14 6 8z", IconRender.Stroke),
            new IconLayer("M9 18a2 2 0 0 0 4 0", IconRender.Stroke)
        },
        "board" => new[]
        {
            new IconLayer(RoundedRect(3, 4, 5.5, 14, 1.4), IconRender.Fill, 0.25),
            new IconLayer(RoundedRect(9, 4, 5.5, 9, 1.4), IconRender.Fill, 0.55),
            new IconLayer(RoundedRect(14.5, 4, 5, 11, 1.4), IconRender.Fill)
        },
        "sparkle" => new[]
        {
            new IconLayer("M11 2l1.8 5.2L18 9l-5.2 1.8L11 16l-1.8-5.2L4 9l5.2-1.8L11 2z", IconRender.Fill),
            new IconLayer("M17 14l.8 2.2L20 17l-2.2.8L17 20l-.8-2.2L14 17l2.2-.8L17 14z", IconRender.Fill, 0.6)
        },
        "chat" => new[]
        {
            new IconLayer(
                "M4 5.5A2 2 0 0 1 6 3.5h10A2 2 0 0 1 18 5.5v8A2 2 0 0 1 16 15.5H9l-4 3v-3H6a2 2 0 0 1-2-2v-8z",
                IconRender.Fill, 0.85)
        },
        "calendar" => new[]
        {
            new IconLayer(RoundedRect(3, 5, 16, 14, 2), IconRender.Stroke),
            new IconLayer("M3 9h16M7 3v4M15 3v4", IconRender.Stroke)
        },
        "home" => new[]
        {
            new IconLayer("M4.5 9.5 L11 3.5 L17.5 9.5 V18 H4.5 Z", IconRender.Stroke),
            new IconLayer("M9 18 V13 H13 V18", IconRender.Stroke)
        },
        "settings" => new[]
        {
            new IconLayer("M8 11 a3 3 0 1 0 6 0 a3 3 0 1 0 -6 0", IconRender.Stroke),
            new IconLayer(
                "M11 2v2 M11 18v2 M2 11h2 M18 11h2 M4.6 4.6l1.4 1.4 " +
                "M16 16l1.4 1.4 M4.6 17.4 L6 16 M16 6l1.4-1.4",
                IconRender.Stroke)
        },
        "micFill" => new[]
        {
            new IconLayer(RoundedRect(8, 3, 6, 11, 3), IconRender.Fill),
            new IconLayer("M5 11 a6 6 0 0 0 12 0 M11 17 v3", IconRender.Stroke)
        },
        "plus" => new[]
        {
            new IconLayer("M11 5v12 M5 11h12", IconRender.Stroke)
        },
        "check" => new[]
        {
            new IconLayer("M5 11.5l4 4 8-8", IconRender.Stroke)
        },
        "swap" => new[]
        {
            new IconLayer("M5 8h12l-3-3 M17 14H5l3 3", IconRender.Stroke)
        },
        "alert" => new[]
        {
            new IconLayer("M11 4.5 L18.5 17 H3.5 Z", IconRender.Stroke),
            new IconLayer("M11 9v3.6 M11 14.8h0.01", IconRender.Stroke)
        },
        _ => System.Array.Empty<IconLayer>()
    };

    private static string RoundedRect(double x, double y, double w, double h, double r)
    {
        static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        return $"M{N(x + r)} {N(y)} " +
               $"H{N(x + w - r)} " +
               $"A{N(r)} {N(r)} 0 0 1 {N(x + w)} {N(y + r)} " +
               $"V{N(y + h - r)} " +
               $"A{N(r)} {N(r)} 0 0 1 {N(x + w - r)} {N(y + h)} " +
               $"H{N(x + r)} " +
               $"A{N(r)} {N(r)} 0 0 1 {N(x)} {N(y + h - r)} " +
               $"V{N(y + r)} " +
               $"A{N(r)} {N(r)} 0 0 1 {N(x + r)} {N(y)} Z";
    }
}
