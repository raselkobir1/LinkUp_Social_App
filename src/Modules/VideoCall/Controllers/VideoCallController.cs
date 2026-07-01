using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.VideoCall.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.VideoCall.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/video-calls")]
[Authorize]
public class VideoCallController(
    IVideoCallManager videoCallManager,
    ITurnCredentialService turnCredentials) : BaseApiController
{
    // GET api/v1/video-calls/ice-servers
    // WebRTC ICE servers (STUN + TURN). TURN credentials are fetched server-side so
    // the Metered API key is never exposed to the browser.
    [HttpGet("ice-servers")]
    public async Task<IActionResult> GetIceServers(CancellationToken ct)
    {
        var result = await turnCredentials.GetIceServersAsync(ct);
        return ApiOk(result);
    }

    // GET api/v1/video-calls/history
    [HttpGet("history")]
    public async Task<IActionResult> GetCallHistory([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await videoCallManager.GetCallHistoryAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    // GET api/v1/video-calls/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCall(CancellationToken ct)
    {
        var result = await videoCallManager.GetActiveCallAsync(CurrentUserId, ct);
        return ApiOk(result);
    }
}
