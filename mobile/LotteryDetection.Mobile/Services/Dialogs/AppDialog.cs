using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LotteryDetection.Mobile.Services.Dialogs;

/// <summary>
///     Centralized dialog helper. Uses native MAUI dialogs so iOS owns dismiss/presentation state.
/// </summary>
public static class AppDialog
{
    private static readonly SemaphoreSlim DialogGate = new(1, 1);

    public static async Task ShowAlertAsync(
        string title,
        string? message = null,
        string ok = "OK")
    {
        var host = ResolveHostPage();
        if (host == null) return;

        await DialogGate.WaitAsync();
        try
        {
            await host.DisplayAlert(title, message ?? string.Empty, ok);
            await Task.Delay(300);
        }
        finally
        {
            DialogGate.Release();
        }
    }

    public static async Task<bool> ShowConfirmAsync(
        string title,
        string? message,
        string acceptText,
        string cancelText = "Cancel",
        bool danger = false,
        string? icon = null,
        string? iconBackground = null)
    {
        var host = ResolveHostPage();
        if (host == null) return false;

        await DialogGate.WaitAsync();
        try
        {
            var confirmed = await host.DisplayAlert(title, message ?? string.Empty, acceptText, cancelText);
            await Task.Delay(300);
            return confirmed;
        }
        finally
        {
            DialogGate.Release();
        }
    }

    public static async Task<string?> ShowPromptAsync(
        string title,
        string? message,
        string accept = "Save",
        string cancel = "Cancel",
        string? initialValue = null,
        string? placeholder = null,
        int maxLength = 256)
    {
        var host = ResolveHostPage();
        if (host == null) return null;

        await DialogGate.WaitAsync();
        try
        {
            var result = await host.DisplayPromptAsync(
                title,
                message ?? string.Empty,
                accept,
                cancel,
                placeholder,
                maxLength,
                initialValue: initialValue);
            await Task.Delay(300);
            return result;
        }
        finally
        {
            DialogGate.Release();
        }
    }

    // Returns the picked option, or null if cancelled.
    public static async Task<string?> ShowActionSheetAsync(
        string title,
        string cancelText,
        params string[] options)
    {
        var host = ResolveHostPage();
        if (host == null) return null;

        await DialogGate.WaitAsync();
        try
        {
            var picked = await host.DisplayActionSheet(title, cancelText, null, options);
            if (string.IsNullOrEmpty(picked) || picked == cancelText) return null;

            await Task.Delay(250);
            return picked;
        }
        finally
        {
            DialogGate.Release();
        }
    }

    private static Page? ResolveHostPage()
    {
        var window = Application.Current?.Windows?.FirstOrDefault();
        return window?.Page ?? Application.Current?.MainPage;
    }
}
