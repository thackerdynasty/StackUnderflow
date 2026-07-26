namespace StackUnderflow.Models;

public class ThreadSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }

    public bool IsSolved { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "unknown user";

    public int AnswerCount { get; set; }
    public int RecentAnswerCount { get; set; }
}
