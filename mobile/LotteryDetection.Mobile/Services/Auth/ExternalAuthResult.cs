namespace LotteryDetection.Mobile.Services.Auth;

/// <summary>
/// Common shape returned by GoogleAuthHelper / FacebookAuthHelper after a
/// successful OAuth round-trip, ready to hand to
/// <see cref="IAuthService.SignInExternalAsync"/>.
/// </summary>
public sealed class ExternalAuthResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string? IdToken { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}
