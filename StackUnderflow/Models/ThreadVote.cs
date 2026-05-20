using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class ThreadVote
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    public int SUThreadId { get; set; }
    public SUThread SUThread { get; set; }
}
