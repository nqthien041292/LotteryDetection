using Microsoft.Maui.Controls;

namespace LotteryDetection.Mobile.Views.Components;

public partial class LotteryBallView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(LotteryBallView), string.Empty);

    public static readonly BindableProperty BallSizeProperty =
        BindableProperty.Create(nameof(BallSize), typeof(double), typeof(LotteryBallView), 32.0);

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(LotteryBallView), 14.0);

    public static readonly BindableProperty StartColorProperty =
        BindableProperty.Create(nameof(StartColor), typeof(Color), typeof(LotteryBallView), Color.FromArgb("#FF4D8D"));

    public static readonly BindableProperty EndColorProperty =
        BindableProperty.Create(nameof(EndColor), typeof(Color), typeof(LotteryBallView), Color.FromArgb("#D90429"));

    public static readonly BindableProperty ShadowColorProperty =
        BindableProperty.Create(nameof(ShadowColor), typeof(Color), typeof(LotteryBallView), Color.FromArgb("#D90429"));

    public LotteryBallView()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double BallSize
    {
        get => (double)GetValue(BallSizeProperty);
        set => SetValue(BallSizeProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double CornerRadius => BallSize / 2.0;

    public Color StartColor
    {
        get => (Color)GetValue(StartColorProperty);
        set => SetValue(StartColorProperty, value);
    }

    public Color EndColor
    {
        get => (Color)GetValue(EndColorProperty);
        set => SetValue(EndColorProperty, value);
    }

    public Color ShadowColor
    {
        get => (Color)GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }
}
