using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.Modules.UserProfile.DTOs;
using LinkUp.Modules.UserProfile.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.UserProfile.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public class ProfileController(IProfileManager profileManager) : BaseApiController
{
    // GET api/v1/profile/{userId}
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        var result = await profileManager.GetProfileAsync(userId, CurrentUserId, ct);
        return ApiOk(result);
    }

    // PUT api/v1/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var result = await profileManager.UpdateProfileAsync(CurrentUserId, dto, ct);
        return ApiOk(result, "Profile updated successfully.");
    }

    // GET api/v1/profile/{userId}/education
    [HttpGet("{userId:guid}/education")]
    public async Task<IActionResult> GetEducations(Guid userId, CancellationToken ct)
    {
        var result = await profileManager.GetEducationsAsync(userId, ct);
        return ApiOk(result);
    }

    // POST api/v1/profile/education
    [HttpPost("education")]
    public async Task<IActionResult> AddEducation([FromBody] CreateEducationDto dto, CancellationToken ct)
    {
        var result = await profileManager.AddEducationAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Education added successfully.");
    }

    // PUT api/v1/profile/education/{id}
    [HttpPut("education/{id:guid}")]
    public async Task<IActionResult> UpdateEducation(Guid id, [FromBody] UpdateEducationDto dto, CancellationToken ct)
    {
        var result = await profileManager.UpdateEducationAsync(CurrentUserId, id, dto, ct);
        return ApiOk(result, "Education updated successfully.");
    }

    // DELETE api/v1/profile/education/{id}
    [HttpDelete("education/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken ct)
    {
        await profileManager.DeleteEducationAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Education deleted successfully.");
    }

    // GET api/v1/profile/{userId}/experience
    [HttpGet("{userId:guid}/experience")]
    public async Task<IActionResult> GetExperiences(Guid userId, CancellationToken ct)
    {
        var result = await profileManager.GetExperiencesAsync(userId, ct);
        return ApiOk(result);
    }

    // POST api/v1/profile/experience
    [HttpPost("experience")]
    public async Task<IActionResult> AddExperience([FromBody] CreateExperienceDto dto, CancellationToken ct)
    {
        var result = await profileManager.AddExperienceAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Experience added successfully.");
    }

    // PUT api/v1/profile/experience/{id}
    [HttpPut("experience/{id:guid}")]
    public async Task<IActionResult> UpdateExperience(Guid id, [FromBody] UpdateExperienceDto dto, CancellationToken ct)
    {
        var result = await profileManager.UpdateExperienceAsync(CurrentUserId, id, dto, ct);
        return ApiOk(result, "Experience updated successfully.");
    }

    // DELETE api/v1/profile/experience/{id}
    [HttpDelete("experience/{id:guid}")]
    public async Task<IActionResult> DeleteExperience(Guid id, CancellationToken ct)
    {
        await profileManager.DeleteExperienceAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Experience deleted successfully.");
    }

    // GET api/v1/profile/{userId}/social-links
    [HttpGet("{userId:guid}/social-links")]
    public async Task<IActionResult> GetSocialLinks(Guid userId, CancellationToken ct)
    {
        var result = await profileManager.GetSocialLinksAsync(userId, ct);
        return ApiOk(result);
    }

    // POST api/v1/profile/social-links
    [HttpPost("social-links")]
    public async Task<IActionResult> AddSocialLink([FromBody] CreateSocialLinkDto dto, CancellationToken ct)
    {
        var result = await profileManager.AddSocialLinkAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Social link added successfully.");
    }

    // DELETE api/v1/profile/social-links/{id}
    [HttpDelete("social-links/{id:guid}")]
    public async Task<IActionResult> DeleteSocialLink(Guid id, CancellationToken ct)
    {
        await profileManager.DeleteSocialLinkAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Social link deleted successfully.");
    }

    // GET api/v1/profile/privacy
    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacySettings(CancellationToken ct)
    {
        var result = await profileManager.GetPrivacySettingsAsync(CurrentUserId, ct);
        return ApiOk(result);
    }

    // PUT api/v1/profile/privacy
    [HttpPut("privacy")]
    public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsDto dto, CancellationToken ct)
    {
        var result = await profileManager.UpdatePrivacySettingsAsync(CurrentUserId, dto, ct);
        return ApiOk(result, "Privacy settings updated successfully.");
    }
}
