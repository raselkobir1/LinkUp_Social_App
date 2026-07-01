using System.Net.Http.Json;
using LinkUp.Modules.VideoCall.Configuration;
using LinkUp.Modules.VideoCall.DTOs;
using LinkUp.Modules.VideoCall.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUp.Modules.VideoCall.Managers;

/// <summary>
/// Fetches WebRTC ICE servers. STUN is always returned; when Metered is configured we
/// call its API server-side (keeping the API key off the client) for time-limited TURN
/// credentials. Any failure falls back to STUN-only so a call is never blocked.
/// </summary>
public class TurnCredentialService(
    HttpClient http,
    IOptions<MeteredOptions> options,
    ILogger<TurnCredentialService> logger) : ITurnCredentialService
{
    // Always available; also the fallback when Metered is unset or unreachable.
    private static readonly List<IceServerDto> StunOnly =
    [
        new() { Urls = ["stun:stun.l.google.com:19302"] }
    ];

    public async Task<List<IceServerDto>> GetIceServersAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.AppName) || string.IsNullOrWhiteSpace(opt.ApiKey))
            return StunOnly;

        try
        {
            var url = $"https://{opt.AppName}.metered.live/api/v1/turn/credentials?apiKey={opt.ApiKey}";
            var turn = await http.GetFromJsonAsync<List<IceServerDto>>(url, ct) ?? [];
            // Google STUN first for redundancy, then Metered's STUN + TURN.
            return [.. StunOnly, .. turn];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Metered TURN credentials; falling back to STUN only.");
            return StunOnly;
        }
    }
}
