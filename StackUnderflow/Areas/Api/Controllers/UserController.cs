using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Areas.Api.Controllers;

[ApiController]
[Area("Api")]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public UserController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto, CancellationToken cancellationToken)
    {
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
