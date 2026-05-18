using Microsoft.Maui.Controls.Shapes;

namespace LotteryDetectionMobile.Views.Components;

/// <summary>
///     Renders a design-bundle vector glyph (authored in a 22-unit viewBox) scaled to
///     <see cref="Size" /> and tinted by <see cref="Color" />. Replaces emoji/symbol
///     text icons that render as .notdef boxes once a text-only font is forced on iOS.
/// </summary>
public sealed class VectorIcon : ContentView
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(VectorIcon), string.Empty,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(VectorIcon), Colors.Black,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(VectorIcon), 18.0,
            propertyChanged: OnVisualChanged);

    private readonly Grid canvas;

    public VectorIcon()
    {
        canvas = new Grid
        {
            WidthRequest = VectorIconPaths.ViewBox,
            HeightRequest = VectorIconPaths.ViewBox,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        Content = canvas;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        Build();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is VectorIcon v) v.Build();
    }

    private void Build()
    {
        canvas.Children.Clear();

        foreach (var layer in VectorIconPaths.For(Glyph ?? string.Empty))
        {
            Geometry? geo;
            try
            {
                geo = (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(layer.Data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VectorIcon: failed to parse '{Glyph}' layer: {ex.Message}");
                geo = null;
            }

            if (geo is null) continue;

            var shape = new Microsoft.Maui.Controls.Shapes.Path
            {
                Data = geo,
                Opacity = layer.Opacity
            };

            if (layer.Render == IconRender.Stroke)
            {
                shape.Stroke = new SolidColorBrush(Color);
                shape.StrokeThickness = VectorIconPaths.StrokeWidth;
                shape.StrokeLineCap = PenLineCap.Round;
                shape.StrokeLineJoin = PenLineJoin.Round;
            }
            else
            {
                shape.Fill = new SolidColorBrush(Color);
            }

            canvas.Children.Add(shape);
        }

        // Paths are authored in the 22-unit viewBox; the canvas keeps that intrinsic
        // size and is render-scaled to Size (Scale is centered, does not affect layout),
        // so it must sit in a container with slack (badges here are 36-40px) to avoid clipping.
        WidthRequest = Size;
        HeightRequest = Size;
        canvas.Scale = Size / VectorIconPaths.ViewBox;
    }
}
