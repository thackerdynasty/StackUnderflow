using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;

namespace StackUnderflow.Controllers;

/// <summary>
/// JSON API for the saved-threads (bookmark) feature. Lets the client toggle a
/// save and refresh the leaderboard without a full page reload.
/// </summary>
[ApiController]
[Route("api/saved-threads")]
public class SavedThreadApiController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    /// <summary>Toggle whether the current user has saved the given thread.</summary>
    [Authorize]
    [HttpPost("{threadId:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int threadId)
    {
        var threadExists = await _context.SUThreads.AnyAsync(t => t.Id == threadId);
        if (!threadExists)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var existing = await _context.SavedThreads
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SUThreadId == threadId);

        bool saved;
        if (existing is null)
        {
            _context.SavedThreads.Add(new SavedThread
            {
                UserId = userId,
                SUThreadId = threadId,
                SavedAt = DateTime.UtcNow
            });
            saved = true;
        }
        else
        {
            _context.SavedThreads.Remove(existing);
            saved = false;
        }

        await _context.SaveChangesAsync();

        return Ok(new { saved });
    }

    /// <summary>Top authors ranked by how many times their threads have been saved.</summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] int count = 3)
    {
        count = Math.Clamp(count, 1, 20);
        var entries = await LeaderboardService.GetTopAuthorsAsync(_context, count);
        return Ok(entries);
    }
}