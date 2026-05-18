using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LotteryDetectionMobile.Models.Family;
using LotteryDetectionMobile.Services.Interfaces;
using LotteryDetectionMobile.Services.Voice;

namespace LotteryDetectionMobile.Services.Family;

public class RemoteFamilyService : IFamilyService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string?>> _tokenProvider;

    public RemoteFamilyService(VoiceApiOptions options, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        _httpClient.BaseAddress = options.BaseUri;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _tokenProvider = options.GetBearerTokenAsync;
    }

    public async Task<FamilyGroupSummary?> GetGroupAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var dto = await _httpClient.GetFromJsonAsync<FamilyGroupResponse>(
                "api/mobile/family/group", JsonOptions);
            if (dto == null) return null;
            return new FamilyGroupSummary
            {
                Id = dto.Id.ToString(),
                Name = dto.Name ?? string.Empty,
                CreatedAt = dto.CreatedAt
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] GetGroupAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<FamilyMember>> GetMembersAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var dtos = await GetListAsync("api/mobile/family/members");
            if (dtos == null) return Enumerable.Empty<FamilyMember>();
            return dtos.Select(MapMember).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] GetMembersAsync failed: {ex.Message}");
            return Enumerable.Empty<FamilyMember>();
        }
    }

    public async Task<IReadOnlyList<RoleOption>> GetRolesAsync()
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RoleOptionResponse>>(
                "api/mobile/family/members/roles", JsonOptions);
            if (dtos == null || dtos.Count == 0) return Array.Empty<RoleOption>();
            return dtos.Select(d => new RoleOption
            {
                Id = d.Id ?? string.Empty,
                Label = d.Label ?? string.Empty,
                Description = d.Description ?? string.Empty,
                TintColor = string.IsNullOrWhiteSpace(d.TintColor) ? "#E5E7EF" : d.TintColor,
                ForegroundColor = string.IsNullOrWhiteSpace(d.ForegroundColor) ? "#334155" : d.ForegroundColor
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] GetRolesAsync failed: {ex.Message}");
            return Array.Empty<RoleOption>();
        }
    }

    public async Task<FamilyMember?> InviteMemberAsync(string emailAddress, string role)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/mobile/family/invitations",
                new InviteFamilyMemberRequest { EmailAddress = emailAddress, Role = role });
            if (!response.IsSuccessStatusCode) return null;

            var member = await ReadMemberAsync(response);
            return member == null ? null : MapMember(member);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] InviteMemberAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<FamilyMember?> UpdateMemberRoleAsync(string memberId, string role)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/mobile/family/members/{memberId}/role",
                new UpdateFamilyMemberRoleRequest { Role = role });
            if (!response.IsSuccessStatusCode) return null;

            var member = await ReadMemberAsync(response);
            return member == null ? null : MapMember(member);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] UpdateMemberRoleAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RemoveMemberAsync(string memberId)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.DeleteAsync($"api/mobile/family/members/{memberId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] RemoveMemberAsync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResendInviteAsync(string memberId)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/mobile/family/members/{memberId}/resend-invite",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] ResendInviteAsync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<FamilyMember?> AcceptInvitationAsync(string token)
    {
        await EnsureAuthHeaderAsync();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/mobile/family/invitations/accept",
                new { token });
            if (!response.IsSuccessStatusCode) return null;

            var member = await ReadMemberAsync(response);
            return member == null ? null : MapMember(member);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RemoteFamilyService] AcceptInvitationAsync failed: {ex.Message}");
            return null;
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
            Console.WriteLine($"[RemoteFamilyService] Failed to set auth header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private class AbpResult<T>
    {
        public T? Result { get; set; }
        public bool Success { get; set; }
    }

    private static FamilyMember MapMember(FamilyMemberResponse d)
    {
        return new FamilyMember
        {
            Id = d.Id,
            Name = d.DisplayName,
            Email = d.EmailAddress,
            Role = d.Role,
            IsOnline = false,
            IsPending = d.IsPending,
            Avatar = string.Empty,
            Points = 0
        };
    }

    private async Task<List<FamilyMemberResponse>?> GetListAsync(string url)
    {
        using var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var direct = JsonSerializer.Deserialize<List<FamilyMemberResponse>>(json, JsonOptions);
            if (direct != null) return direct;
        }
        catch
        {
            // Fall through to ABP-wrapped shape.
        }

        var wrapped = JsonSerializer.Deserialize<AbpResult<List<FamilyMemberResponse>>>(json, JsonOptions);
        return wrapped?.Result;
    }

    private static async Task<FamilyMemberResponse?> ReadMemberAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var direct = JsonSerializer.Deserialize<FamilyMemberResponse>(json, JsonOptions);
            if (direct != null) return direct;
        }
        catch
        {
            // Fall through to ABP-wrapped shape.
        }

        var wrapped = JsonSerializer.Deserialize<AbpResult<FamilyMemberResponse>>(json, JsonOptions);
        return wrapped?.Result;
    }

    private class InviteFamilyMemberRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
    }

    private class UpdateFamilyMemberRoleRequest
    {
        public string Role { get; set; } = "Member";
    }

    private class FamilyMemberResponse
    {
        public string Id { get; set; } = string.Empty;
        public string UserIdString { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public bool IsActive { get; set; }
        public bool IsPending { get; set; }
        public bool IsAbpUser { get; set; }
    }

    private class FamilyGroupResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class RoleOptionResponse
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? Description { get; set; }
        public string? TintColor { get; set; }
        public string? ForegroundColor { get; set; }
    }
}
