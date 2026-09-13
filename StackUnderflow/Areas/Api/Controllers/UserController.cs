using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Areas.Api.Controllers;

[ApiController]
[Area("Api")]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserController(
    ApplicationDbContext dbContext,
    IProfileImageStorage profileImageStorage) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IProfileImageStorage _profileImageStorage = profileImageStorage;

    /// <summary>
    /// The signed-in user's id, or null when the request is anonymous or the principal
    /// carries no subject claim. [Authorize] already covers the first case; checking
    /// here too keeps the rule assertable without middleware.
    /// </summary>
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Explicit 403 rather than Forbid(), which cookie authentication would turn into a
    /// redirect to the login page — not useful to an API caller.
    /// </summary>
    private IActionResult ForbidOtherAccount(string action) => Problem(
        title: "Forbidden",
        detail: $"You can only {action} your own account.",
        statusCode: StatusCodes.Status403Forbidden);

    // GET: /api/user
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => ToDto(u))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    // GET: /api/user/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToDto(user));
    }

    // PUT: /api/user/{id}
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        if (!string.Equals(id, currentUserId, StringComparison.Ordinal))
        {
            return ForbidOtherAccount("update");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (dto.UserName is not null)
        {
            user.UserName = dto.UserName;
        }

        if (dto.Email is not null)
        {
            user.Email = dto.Email;
        }

        if (dto.ProfilePicture is not null)
        {
            user.ProfilePicture = dto.ProfilePicture;
        }

        if (dto.Bio is not null)
        {
            user.Bio = dto.Bio;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE: /api/user/{id}
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        if (!string.Equals(id, currentUserId, StringComparison.Ordinal))
        {
            return ForbidOtherAccount("delete");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // Captured before the entity is removed, since the row is the only record of
        // where the image lives.
        var profileImagePath = user.ProfileImagePath;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Deleted after the row is gone, so a storage failure leaves at most an orphaned
        // blob rather than a live user pointing at a deleted image. DeleteAsync already
        // tolerates a null path and swallows storage errors.
        await _profileImageStorage.DeleteAsync(profileImagePath, cancellationToken);

        return NoContent();
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        Reputation = user.Reputation,
        JoinDate = user.JoinDate,
        ProfilePicture = user.ProfilePicture,
        Bio = user.Bio,
    };
}
