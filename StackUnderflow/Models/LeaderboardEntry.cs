namespace StackUnderflow.Models;

/// <summary>
/// One row of the home-page leaderboard: a user ranked by how many times their
/// authored threads have been saved by other users.
/// </summary>
public class LeaderboardEntry
{
    public string UserId { get; set; } = "";

    /// <summary>Display name (username with any email domain stripped off).</summary>
    public string Name { get; set; } = "";

    /// <summary>Single-letter avatar initial derived from <see cref="Name"/>.</summary>
    public string Initials { get; set; } = "?";

    /// <summary>
    /// Avatar to render: an image the user uploaded wins, then the seeded external
    /// picture, then null so the row falls back to <see cref="Initials"/>. Resolved
    /// the same way as the profile page and hover card so one user looks identical
    /// everywhere.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Number of times this user's threads have been saved by others.</summary>
    public int SaveCount { get; set; }
}