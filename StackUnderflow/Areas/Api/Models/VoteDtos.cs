namespace StackUnderflow.Areas.Api.Models;
public record VoteRequest(string Direction);

public record VoteResult
{
    public int ThreadId { get; init; }
    public int Score { get; init; } = 0;
    public int UpvoteCount { get; init; } = 0;
    public int DownvoteCount { get; init; } = 0;
    public int UserVote { get; init; }       // 1, -1, or 0 (after toggle-off)
}

// Same shape as VoteResult but keyed by the answer (post) instead of the thread.
public record PostVoteResult
{
    public int PostId { get; init; }
    public int Score { get; init; } = 0;
    public int UpvoteCount { get; init; } = 0;
    public int DownvoteCount { get; init; } = 0;
    public int UserVote { get; init; }       // 1, -1, or 0 (after toggle-off)
}
