using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Services;

// Mirror of ThreadVoteService for answers (Post). Same add / switch / toggle-off
// rules and self-vote guard, but against PostVotes and the Post's own counters.
public class PostVoteService
{
    private readonly ApplicationDbContext _context;

    public PostVoteService(ApplicationDbContext context) => _context = context;

    public static int? ParseVoteValue(string vote) => ThreadVoteService.ParseVoteValue(vote);

    public async Task<PostVoteOutcome> VoteAsync(
        int postId, string userId, int voteValue, CancellationToken ct = default)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null)
            return new PostVoteOutcome(PostVoteStatus.PostNotFound, postId, 0, 0, 0);

        // A user cannot vote on their own answer.
        if (post.UserId == userId)
            return new PostVoteOutcome(
                PostVoteStatus.SelfVoteNotAllowed, postId,
                post.Upvotes, post.Downvotes, 0);

        var existing = await _context.PostVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.PostId == postId, ct);

        int userVote;
        if (existing is null)
        {
            ApplyPostVote(post, voteValue);
            _context.PostVotes.Add(new PostVote
            {
                Value = voteValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId,
                PostId = postId
            });
            userVote = voteValue;
        }
        else if (existing.Value != voteValue)   // switch direction
        {
            RevertPostVote(post, existing.Value);
            ApplyPostVote(post, voteValue);
            existing.Value = voteValue;
            existing.UpdatedAt = DateTime.UtcNow;
            userVote = voteValue;
        }
        else                                    // same direction -> toggle off
        {
            RevertPostVote(post, existing.Value);
            _context.PostVotes.Remove(existing);
            userVote = 0;
        }

        await _context.SaveChangesAsync(ct);

        return new PostVoteOutcome(
            PostVoteStatus.Success, post.Id,
            post.Upvotes, post.Downvotes, userVote);
    }

    private static void ApplyPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
        {
            post.Upvotes++;
            post.User.Reputation += 10;
        }
        else
        {
            post.Downvotes++;
            post.User.Reputation -= 2;
        }
    }

    private static void RevertPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
        {
            post.Upvotes = Math.Max(0, post.Upvotes - 1);
            post.User.Reputation -= 10;
        }
        else
        {
            post.Downvotes = Math.Max(0, post.Downvotes - 1);
            post.User.Reputation += 2;
        }
    }
}

public enum PostVoteStatus
{
    Success,
    PostNotFound,
    SelfVoteNotAllowed
}

public record PostVoteOutcome(
    PostVoteStatus Status,
    int PostId,
    int UpvoteCount,
    int DownvoteCount,
    int UserVote)
{
    public int Score => UpvoteCount - DownvoteCount;
}
