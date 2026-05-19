using System.Threading.Tasks;

namespace LotteryDetection.Mobile.Views.Components;

public enum AppDialogMode
{
    Alert,
    Confirm,
    Prompt
}

/// <summary>
///     Transparent modal that mimics the delete-confirmation card style across the app.
///     Resolves the <see cref="TaskCompletionSource{Result}" /> when the user taps a button or backdrop.
/// </summary>
public partial class AppDialogPage : ContentPage
{
    private readonly TaskCompletionSource<DialogResult> _tcs = new();
    private bool _closeOnBackdrop = true;
    private bool _completed;

    public AppDialogPage()
    {
        InitializeComponent();
    }

    public Task<DialogResult> Result => _tcs.Task;

    public void Configure(
        AppDialogMode mode,
        string title,
        string? message,
        string acceptText,
        string? cancelText,
        bool danger = false,
        string? icon = null,
        string? iconBackground = null,
        string? initialInput = null,
        string? placeholder = null,
        int maxLength = 256,
        bool closeOnBackdrop = true)
    {
        TitleLabel.Text = title;

        if (string.IsNullOrWhiteSpace(message))
        {
            MessageLabel.IsVisible = false;
        }
        else
        {
            MessageLabel.IsVisible = true;
            MessageLabel.Text = message;
        }

        // Icon
        if (!string.IsNullOrWhiteSpace(icon))
        {
            IconHost.IsVisible = true;
            IconLabel.Text = icon;
            if (!string.IsNullOrWhiteSpace(iconBackground)
                && Color.TryParse(iconBackground, out var bg))
            {
                IconHost.BackgroundColor = bg;
            }
        }

        // Input (prompt mode only)
        if (mode == AppDialogMode.Prompt)
        {
            InputHost.IsVisible = true;
            InputEntry.Text = initialInput ?? string.Empty;
            InputEntry.Placeholder = placeholder ?? string.Empty;
            InputEntry.MaxLength = maxLength;
        }

        // Buttons
        AcceptButton.Text = acceptText;

        if (mode == AppDialogMode.Alert || string.IsNullOrEmpty(cancelText))
        {
            // Single-button layout — hide cancel, stretch accept across the row.
            CancelButton.IsVisible = false;
            ButtonRow.ColumnDefinitions.Clear();
            ButtonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            Grid.SetColumn(AcceptButton, 0);
        }
        else
        {
            CancelButton.Text = cancelText;
        }

        if (danger)
        {
            AcceptButton.BackgroundColor = Color.FromArgb("#EF4444");
        }

        _closeOnBackdrop = closeOnBackdrop;
    }

    private const uint EnterDurationMs = 180;
    private const uint ExitDurationMs = 140;
    private const int CloseTimeoutMs = 1200;
    private const double BackdropOpacity = 0.55;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Enter animation: backdrop fades in while the card scales up from 0.85 with a slight overshoot.
        var backdropFade = Backdrop.FadeTo(BackdropOpacity, EnterDurationMs, Easing.CubicOut);
        var cardFade = Card.FadeTo(1, EnterDurationMs, Easing.CubicOut);
        var cardScale = Card.ScaleTo(1, EnterDurationMs, Easing.SpringOut);
        await Task.WhenAll(backdropFade, cardFade, cardScale);

        if (InputHost.IsVisible)
        {
            // Auto-focus the entry so users can type immediately.
            InputEntry.Focus();
        }
    }

    private async void OnAcceptClicked(object? sender, EventArgs e)
    {
        await CloseAsync(new DialogResult(true, InputEntry?.Text));
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(new DialogResult(false, null));
    }

    private async void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        if (!_closeOnBackdrop) return;
        await CloseAsync(new DialogResult(false, null));
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        // Intentionally empty — swallows backdrop tap propagation when interacting with the card.
    }

    private async Task CloseAsync(DialogResult result)
    {
        if (_completed) return;
        _completed = true;

        try
        {
            // Exit animation: card scales down + fades while backdrop fades out, then pop modal.
            var backdropFade = Backdrop.FadeTo(0, ExitDurationMs, Easing.CubicIn);
            var cardFade = Card.FadeTo(0, ExitDurationMs, Easing.CubicIn);
            var cardScale = Card.ScaleTo(0.9, ExitDurationMs, Easing.CubicIn);
            await RunWithTimeout(Task.WhenAll(backdropFade, cardFade, cardScale), CloseTimeoutMs);

            await RunWithTimeout(Navigation.PopModalAsync(false), CloseTimeoutMs);
        }
        catch
        {
            // Navigation may already have been torn down (e.g. parent disposed). Resolve anyway.
        }
        finally
        {
            _tcs.TrySetResult(result);
        }
    }

    private static async Task RunWithTimeout(Task task, int timeoutMs)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
        if (completed == task)
        {
            await task;
        }
    }
}

public sealed record DialogResult(bool Accepted, string? Text);
