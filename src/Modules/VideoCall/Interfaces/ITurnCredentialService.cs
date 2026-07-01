using LinkUp.Modules.VideoCall.DTOs;

namespace LinkUp.Modules.VideoCall.Interfaces;

public interface ITurnCredentialService
{
    /// <summary>
    /// Returns the ICE servers for WebRTC. Always includes STUN; appends Metered's
    /// TURN servers (with fresh credentials) when Metered is configured. Falls back to
    /// STUN-only if Metered is unreachable, so calls are never blocked.
    /// </summary>
    Task<List<IceServerDto>> GetIceServersAsync(CancellationToken ct = default);
}
