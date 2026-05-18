using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Dashboard;
using Microsoft.AspNetCore.SignalR.Client;

namespace LotteryDetectionMobile.Services.Dashboard;

public class SignalRDashboardClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public SignalRDashboardClient(string baseUrl, Func<Task<string?>>? tokenProvider)
    {
        var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/dashboard";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (tokenProvider != null)
                    options.AccessTokenProvider = async () => await tokenProvider();
                options.Headers["X-User-Timezone"] = TimeZoneInfo.Local.Id;
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            })
            .Build();

        RegisterHandlers();
        RegisterConnectionEvents();
    }

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public event Action<DashboardMetrics>? OnDashboardUpdated;
    public event Action<HubConnectionState>? OnConnectionStateChanged;

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private void RegisterHandlers()
    {
        _connection.On<DashboardMetrics>("OnDashboardUpdated", metrics =>
        {
            Debug.WriteLine($"[DashboardHub] Metrics received: Open={metrics.OpenTasks}, DueToday={metrics.DueToday}, Completed={metrics.Completed}");
            OnDashboardUpdated?.Invoke(metrics);
        });
    }

    private void RegisterConnectionEvents()
    {
        _connection.Reconnecting += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Disconnected) return;

        try
        {
            await _connection.StartAsync(ct);
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            Debug.WriteLine("[DashboardHub] Connected");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardHub] Connect failed: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected) return;
        await _connection.StopAsync();
        Debug.WriteLine("[DashboardHub] Disconnected");
    }
}
