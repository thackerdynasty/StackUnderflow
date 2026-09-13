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

    /// <summary>
    /// Absolute URL of an uploaded avatar, composed at read time from the relative
    /// path in the database. Null when the user has not uploaded one, or when no
    /// image storage is configured.
    /// </summary>
    public string? UploadedImageUrl { get; set; }

    /// <summary>
    /// The avatar to render: an uploaded image wins, then the external
    /// <see cref="ProfilePicture"/> URL, then null so the view shows initials.
    /// </summary>
    public string? AvatarUrl => UploadedImageUrl ?? User.ProfilePicture?.ToString();

    /// <summary>
    /// True only on your own profile and only when image storage is configured, so
    /// the upload control is hidden rather than offered and then rejected with a 503.
    /// </summary>
    public bool CanUploadImage { get; set; }

    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
    public int AcceptedAnswerCount { get; set; }
    public int CommentCount { get; set; }
    public int SavedThreadCount { get; set; }

    // Activity lists
    public IReadOnlyList<SUThread> Questions { get; set; } = [];
    public IReadOnlyList<Post> Answers { get; set; } = [];
    public IReadOnlyList<Comment> Comments { get; set; } = [];
    public IReadOnlyList<SUThread> SavedThreads { get; set; } = [];

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