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
public class VideoCallController(IVideoCallManager videoCallManager) : BaseApiController
{
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
