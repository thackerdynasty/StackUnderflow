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

    /// <summary>Number of times this user's threads have been saved by others.</summary>
    public int SaveCount { get; set; }
}