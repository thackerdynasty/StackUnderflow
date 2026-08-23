using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class ThreadReport
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = "";

    [MaxLength(1000)]
    public string? Details { get; set; }

    public DateTime ReportedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public ThreadReportResolution? Resolution { get; set; }

    [MaxLength(1000)]
    public string? ModeratorNotes { get; set; }

    public string ReporterId { get; set; } = "";
    public User Reporter { get; set; } = null!;

    public string? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }

    public int SUThreadId { get; set; }
    public SUThread SUThread { get; set; } = null!;
}

public enum ThreadReportResolution
{
    Safe = 1,
    Unsafe = 2
}

public static class ThreadReportReasons
{
    public const string Spam = "Spam";
    public const string Harassment = "Harassment or abuse";
    public const string InappropriateContent = "Inappropriate content";
    public const string Misinformation = "Misinformation";
    public const string Other = "Other";

    public static IReadOnlyList<string> All { get; } =
    [
        Spam,
        Harassment,
        InappropriateContent,
        Misinformation,
        Other
    ];
}
