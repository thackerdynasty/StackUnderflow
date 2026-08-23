using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class ThreadReportViewModel
{
    public int ThreadId { get; set; }
    public string ThreadTitle { get; set; } = "";

    [Required(ErrorMessage = "Select a reason for reporting this thread.")]
    public string Reason { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Additional details cannot exceed 1000 characters.")]
    [Display(Name = "Additional details")]
    public string? Details { get; set; }
}
