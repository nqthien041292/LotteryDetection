using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Configuration;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Gamification;

public class RemoteRewardService : IRewardService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteRewardService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(DevHttpsHelper.CreateHandler());
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<IEnumerable<Reward>> GetRewardsAsync(int availableXp)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var list = await _httpClient.GetFromJsonAsync<List<RewardResponse>>(
                "api/mobile/rewards", JsonOptions);
            if (list == null) return Enumerable.Empty<Reward>();
            return list.Select(r => new Reward
            {
                Id = r.Id,
                Emoji = r.Emoji,
                Name = r.Name,
                Cost = r.CostXp,
                Affordable = r.Affordable
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteRewardService] GetRewardsAsync failed: {ex.Message}");
            return Enumerable.Empty<Reward>();
        }
    }

    public async Task<RedeemResult> RedeemRewardAsync(string rewardId)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsync($"api/mobile/rewards/{rewardId}/redeem", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RedeemResponse>(JsonOptions);
                return new RedeemResult(true, result?.AvailablePoints ?? 0);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new RedeemResult(false, 0, error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteRewardService] RedeemRewardAsync failed: {ex.Message}");
            return new RedeemResult(false, 0, ex.Message);
        }
    }

    private async Task EnsureAuthHeaderAsync()
    {
        try
        {
            var token = await _tokenProvider();
            _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteRewardService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class RewardResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CostXp { get; set; }
        public bool Affordable { get; set; }
    }

    private class RedeemResponse
    {
        public int AvailablePoints { get; set; }
        public string RedeemedRewardId { get; set; } = string.Empty;
    }
}
