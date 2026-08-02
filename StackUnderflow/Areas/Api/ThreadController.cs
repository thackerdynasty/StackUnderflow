using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Data;
using StackUnderflow.Extensions;
using StackUnderflow.Models;
using StackUnderflow.Services;
using System.Security.Claims;

namespace StackUnderflow.Areas.Api;

[Route("api/[controller]")]
[ApiController]
public class ThreadController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ThreadVoteService _voteService;

    public ThreadController(ApplicationDbContext context, ThreadVoteService voteService)
    {
        _context = context;
        _voteService = voteService;
    }

    // GET: api/Thread
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SUThread>>> GetSUThreads()
    {
        return await _context.SUThreads.ToListAsync();
    }

    [HttpGet("paginated")]
    public async Task<ActionResult<PaginatedResponse<ThreadSummaryDto>>> GetSUThreadsPaginated(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] string? search = null,
        [FromQuery] string? filter = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var recentCutoff = DateTime.UtcNow.AddDays(-7);

        var threads = _context.SUThreads.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            threads = threads.Where(t => t.Title.Contains(search) || t.Content.Contains(search));
        }

        // Mirrors the home-page filter tabs; Id as a stable tiebreak so pages
        // don't overlap or skip.
        var ordered = filter switch
        {
            "trending" => threads
                .Where(t => t.Posts.Any(post => post.CreatedAt >= recentCutoff))
                .OrderByDescending(t => t.Posts.Count(post => post.CreatedAt >= recentCutoff))
                .ThenByDescending(t => t.UpvoteCount - t.DownvoteCount)
                .ThenByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id),
            "viewed" => threads
                .OrderByDescending(t => t.ViewCount)
                .ThenByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id),
            // Sort by net score (up - down) to match the "votes" number shown on each card.
            "upvoted" => threads
                .OrderByDescending(t => t.UpvoteCount - t.DownvoteCount)
                .ThenByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id),
            _ => threads
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
        };

        var query = ordered
            .Select(t => new ThreadSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Content = t.Content,
                CreatedAt = t.CreatedAt,
                ViewCount = t.ViewCount,
                UpvoteCount = t.UpvoteCount,
                DownvoteCount = t.DownvoteCount,
                IsSolved = t.IsSolved,
                UserId = t.UserId,
                AuthorName = t.User != null ? t.User.UserName : "unknown user",
                AnswerCount = t.Posts.Count,
                RecentAnswerCount = t.Posts.Count(post => post.CreatedAt >= recentCutoff)
            });

        var result = await query.ToPaginatedResponseAsync(pagination);
        return Ok(result);
    }

    // GET: api/Thread/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SUThread>> GetSUThread(int id)
    {
        var sUThread = await _context.SUThreads.FindAsync(id);

        if (sUThread == null)
        {
            return NotFound();
        }

        return sUThread;
    }

    // PUT: api/Thread/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutSUThread(int id, SUThread sUThread)
    {
        if (id != sUThread.Id)
        {
            return BadRequest();
        }

        _context.Entry(sUThread).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SUThreadExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Thread
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SUThread>> PostSUThread(SUThread sUThread)
    {
        _context.SUThreads.Add(sUThread);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSUThread", new { id = sUThread.Id }, sUThread);
    }

    // DELETE: api/Thread/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteSUThread(int id)
    {
        var sUThread = await _context.SUThreads.FindAsync(id);
        if (sUThread == null)
        {
            return NotFound();
        }

        _context.SUThreads.Remove(sUThread);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/Thread/5/vote  ->  { "direction": "up" | "down" }
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoteResult>> Vote(int id, VoteRequest request, CancellationToken ct)
    {
        var value = ThreadVoteService.ParseVoteValue(request.Direction);
        if (value is null)
        {
            return BadRequest("Direction must be 'up' or 'down'.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var outcome = await _voteService.VoteAsync(id, userId, value.Value, ct);

        return outcome.Status switch
        {
            ThreadVoteStatus.ThreadNotFound => NotFound(),
            ThreadVoteStatus.SelfVoteNotAllowed =>
                StatusCode(StatusCodes.Status403Forbidden, "You cannot vote on your own thread."),
            _ => Ok(new VoteResult
            {
                ThreadId = outcome.ThreadId,
                Score = outcome.Score,
                UpvoteCount = outcome.UpvoteCount,
                DownvoteCount = outcome.DownvoteCount,
                UserVote = outcome.UserVote
            })
        };
    }

    private bool SUThreadExists(int id)
    {
        return _context.SUThreads.Any(e => e.Id == id);
    }
}