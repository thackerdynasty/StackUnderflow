using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Services;

public class ThreadVoteService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public static int? ParseVoteValue(string vote) => vote switch
    {
        "up" => 1,
        "down" => -1,
        _ => null
    };
    public async Task<ThreadVoteOutcome> VoteAsync(
        int threadId, string userId, int voteValue, CancellationToken ct = default)
    {
        var thread = await _context.SUThreads
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);
        if (thread is null)
            return new ThreadVoteOutcome(ThreadVoteStatus.ThreadNotFound, threadId, 0, 0, 0);

        if (thread.UserId == userId)
            return new ThreadVoteOutcome(
                ThreadVoteStatus.SelfVoteNotAllowed, threadId,
                thread.UpvoteCount, thread.DownvoteCount, 0);

        var existing = await _context.ThreadVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.SUThreadId == threadId, ct);

        int userVote;
        if (existing is null)
        {
            ApplyThreadVote(thread, voteValue);
            _context.ThreadVotes.Add(new ThreadVote
            {
                Value = voteValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId,
                SUThreadId = threadId
            });
            userVote = voteValue;
        }
        else if (existing.Value != voteValue)   // switch direction
        {
            RevertThreadVote(thread, existing.Value);
            ApplyThreadVote(thread, voteValue);
            existing.Value = voteValue;
            existing.UpdatedAt = DateTime.UtcNow;
            userVote = voteValue;
        }
        else                                    // same direction -> toggle off
        {
            RevertThreadVote(thread, existing.Value);
            _context.ThreadVotes.Remove(existing);
            userVote = 0;
        }

        await _context.SaveChangesAsync(ct);

        return new ThreadVoteOutcome(
            ThreadVoteStatus.Success, thread.Id,
            thread.UpvoteCount, thread.DownvoteCount, userVote);
    }

    private static void ApplyThreadVote(SUThread thread, int voteValue)
    {
        if (voteValue > 0)
        {
            thread.UpvoteCount++;
            thread.User.Reputation += 10;
        }
        else
        {
            thread.DownvoteCount++;
            thread.User.Reputation -= 2;
        }
    }

    private static void RevertThreadVote(SUThread thread, int voteValue)
    {
        if (voteValue > 0)
        {
            thread.UpvoteCount = Math.Max(0, thread.UpvoteCount - 1);
            thread.User.Reputation -= 10;
        }
        else
        {
            thread.DownvoteCount = Math.Max(0, thread.DownvoteCount - 1);
            thread.User.Reputation += 2;
        }
    }
}

public enum ThreadVoteStatus
{
    Success,
    ThreadNotFound,
    SelfVoteNotAllowed
}

public record ThreadVoteOutcome(
    ThreadVoteStatus Status,
    int ThreadId,
    int UpvoteCount,
    int DownvoteCount,
    int UserVote)
{
    public int Score => UpvoteCount - DownvoteCount;
}
