using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Dashboard;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Dashboard;

public class SignalRDashboardService : IDashboardRealtimeService, IAsyncDisposable
{
    private readonly SignalRDashboardClient _client;

    public SignalRDashboardService(VoiceApiOptions options)
    {
        _client = new SignalRDashboardClient(options.BaseUrl, options.GetBearerTokenAsync);
        _client.OnDashboardUpdated += metrics => DashboardUpdated?.Invoke(this, metrics);
    }

    public event EventHandler<DashboardMetrics>? DashboardUpdated;

    public async Task ConnectAsync()
    {
        try
        {
            await _client.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardService] ConnectAsync failed: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardService] DisconnectAsync failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
