namespace LotteryDetectionMobile.Behaviors;

/// <summary>
///     Attaches a press-bounce animation to any tappable View (Border, Grid, Frame, etc.).
///     Adds its own TapGestureRecognizer so existing command gestures are unaffected.
/// </summary>
public class TapAnimationBehavior : Behavior<View>
{
    private View? _view;
    private readonly TapGestureRecognizer _tap = new();

    protected override void OnAttachedTo(View bindable)
    {
        _view = bindable;
        _tap.Tapped += OnTapped;
        bindable.GestureRecognizers.Add(_tap);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        _tap.Tapped -= OnTapped;
        bindable.GestureRecognizers.Remove(_tap);
        _view = null;
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (_view == null) return;
        try
        {
            await _view.ScaleTo(0.94, 75, Easing.CubicOut);
            await _view.ScaleTo(1.0, 150, Easing.SpringOut);
        }
        catch
        {
            // Element unloaded mid-animation — ignore.
        }
    }
}
