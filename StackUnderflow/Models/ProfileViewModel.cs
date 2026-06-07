namespace StackUnderflow.Models;

/// <summary>
/// Data shown on the signed-in user's profile page (StackOverflow-style summary).
/// </summary>
public class ProfileViewModel
{
    public required User User { get; set; }

    /// <summary>True when the signed-in user is viewing their own profile (enables bio editing, etc.).</summary>
    public bool IsOwnProfile { get; set; }

    // Headline stats
    public int Reputation => User.Reputation;
    public DateTime JoinDate => User.JoinDate;
    public Uri? ProfilePicture => User.ProfilePicture;
    public string? Bio => User.Bio;

    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
    public int AcceptedAnswerCount { get; set; }
    public int CommentCount { get; set; }

    // Activity lists
    public IReadOnlyList<SUThread> Questions { get; set; } = [];
    public IReadOnlyList<Post> Answers { get; set; } = [];
    public IReadOnlyList<Comment> Comments { get; set; } = [];

    /// <summary>A short display name derived from the username/email for the avatar + heading.</summary>
    public string DisplayName
    {
        get
        {
            var name = User.UserName ?? User.Email ?? "User";
            var at = name.IndexOf('@');
            return at > 0 ? name[..at] : name;
        }
    }

    public string Initials
    {
        get
        {
            var name = DisplayName.Trim();
            return string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant();
        }
    }
}