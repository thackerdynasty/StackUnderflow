using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class ThreadTag
{
    public int SUThreadId { get; set; }
    public SUThread SUThread { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
