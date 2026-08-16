using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Services;
using System.Security.Claims;

namespace StackUnderflow.Areas.Api.Controllers;

[ApiController]
[Area("Api")]
[Route("api/[controller]")]
[Produces("application/json")]
public class PostController(PostVoteService voteService) : ControllerBase
{
    private readonly PostVoteService _voteService = voteService;

    // POST: api/Post/5/vote  ->  { "direction": "up" | "down" }
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostVoteResult>> Vote(int id, VoteRequest request, CancellationToken ct)
    {
        var value = PostVoteService.ParseVoteValue(request.Direction);
        if (value is null)
        {
            return BadRequest("Direction must be 'up' or 'down'.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var outcome = await _voteService.VoteAsync(id, userId, value.Value, ct);

        return outcome.Status switch
        {
            PostVoteStatus.PostNotFound => NotFound(),
            PostVoteStatus.SelfVoteNotAllowed =>
                StatusCode(StatusCodes.Status403Forbidden, "You cannot vote on your own post."),
            _ => Ok(new PostVoteResult
            {
                PostId = outcome.PostId,
                Score = outcome.Score,
                UpvoteCount = outcome.UpvoteCount,
                DownvoteCount = outcome.DownvoteCount,
                UserVote = outcome.UserVote
            })
        };
    }
}
