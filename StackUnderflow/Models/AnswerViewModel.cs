namespace StackUnderflow.Models;

// Carries a single answer plus the surrounding thread/user context needed to render
// the answer card. Used both by the Thread detail view and the paginated "Load more
// answers" partial so the markup (and anti-forgery tokens) is defined in one place.
public class AnswerViewModel
{
    public Post Post { get; set; } = null!;
    public int ThreadId { get; set; }
    public string ThreadUserId { get; set; } = string.Empty;
    public bool ThreadIsSolved { get; set; }
    public bool IsThreadOwner { get; set; }
    public string? CurrentUserId { get; set; }
    public int AnswerVote { get; set; }
    public int? EditPostId { get; set; }
    public int? EditCommentId { get; set; }
}
