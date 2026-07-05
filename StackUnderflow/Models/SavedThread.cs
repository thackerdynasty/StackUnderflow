using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

// Join entity recording that a user has saved (bookmarked) a thread.
// One row per (user, thread) pair — enforced by a unique index in ApplicationDbContext.
public class SavedThread
{
    [Key]
    public int Id { get; set; }

    public DateTime SavedAt { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    public int SUThreadId { get; set; }
    public SUThread SUThread { get; set; }
}
