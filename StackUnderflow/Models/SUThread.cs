using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class SUThread
{
    [Key]
    public int Id { get; set; }
    
    public string Title { get; set; } = "Thread Title";
    public string Content { get; set; } = "Thread Content";
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    
    public bool IsSolved { get; set; }
    public bool IsLocked { get; set; }

    // When the thread was marked solved (an answer accepted). Null when unsolved.
    // Used to auto-lock threads that have stayed solved past the retention window.
    public DateTime? SolvedAt { get; set; }

    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;
    
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<ThreadVote> Votes { get; set; } = [];
    public ICollection<SavedThread> SavedBy { get; set; } = [];
    public ICollection<ThreadTag> ThreadTags { get; set; } = [];
    public ICollection<ThreadReport> Reports { get; set; } = [];
}
