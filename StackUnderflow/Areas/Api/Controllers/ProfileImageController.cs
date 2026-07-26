using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Data;
using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Areas.Api.Controllers;

/// <summary>
/// Upload endpoint for user avatars. Kept separate from <see cref="UserController"/>
/// because this one is authenticated and accepts multipart/form-data rather than JSON.
/// Inert until Azure Storage is configured, in which case it answers 503.
/// </summary>
[ApiController]
[Area("Api")]
[Route("api/user/{id}/profile-image")]
[Produces("application/json")]
public class ProfileImageController(
    ApplicationDbContext dbContext,
    IProfileImageStorage storage,
    ILogger<ProfileImageController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IProfileImageStorage _storage = storage;
    private readonly ILogger<ProfileImageController> _logger = logger;

    // POST: /api/user/{id}/profile-image
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload(string id, IFormFile? file, CancellationToken cancellationToken)
    {
        // [Authorize] already blocks anonymous callers; this also covers a principal
        // with no subject claim and keeps the rule assertable without middleware.
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // A user may only change their own avatar. Explicit 403 rather than Forbid(),
        // which cookie authentication would turn into a redirect to the login page.
        if (!string.Equals(id, currentUserId, StringComparison.Ordinal))
        {
            return Problem(
                title: "Forbidden",
                detail: "You can only change your own profile image.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        // Checked before reading the body so an unconfigured deployment does not make
        // callers upload megabytes only to be told the feature is switched off.
        if (!_storage.IsConfigured)
        {
            return Problem(
                title: "Profile image storage is not configured",
                detail: "Profile image uploads are disabled because Azure Storage has not been configured yet. "
                        + "See the \"Profile image uploads\" section of the README for setup steps.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (file is null || file.Length == 0)
        {
            return Problem(
                title: "No image supplied",
                detail: "Attach an image using the 'file' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validation runs entirely on the file's own bytes, before any storage call.
        await using var content = file.OpenReadStream();
        var validation = await ProfileImageValidator.ValidateAsync(content, file.Length, cancellationToken);
        if (!validation.IsValid)
        {
            return Problem(
                title: "Invalid image",
                detail: validation.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var previousPath = user.ProfileImagePath;

        string relativePath;
        try
        {
            relativePath = await _storage.UploadAsync(
                currentUserId,
                content,
                validation.ContentType!,
                validation.Extension!,
                cancellationToken);
        }
        catch (ProfileImageStorageException ex)
        {
            _logger.LogError(ex, "Profile image upload failed for user {UserId}.", currentUserId);
            return Problem(
                title: "Upload failed",
                detail: "The image could not be stored. Please try again, or check the storage configuration.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        user.ProfileImagePath = relativePath;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Only after the new path is safely persisted, so a failure here can never
        // leave the user pointing at a blob that has been deleted.
        if (!string.IsNullOrWhiteSpace(previousPath) && previousPath != relativePath)
        {
            await _storage.DeleteAsync(previousPath, cancellationToken);
        }

        return Ok(new ProfileImageUploadedDto
        {
            ProfileImagePath = relativePath,
            ProfileImageUrl = _storage.GetUrl(relativePath),
        });
    }
}