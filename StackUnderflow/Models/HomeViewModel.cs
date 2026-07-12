namespace StackUnderflow.Models;

/// <summary>
/// Data shown on the home page: the top questions list plus the reputation leaderboard.
/// </summary>
public class HomeViewModel
{
    public IReadOnlyList<SUThread> Threads { get; set; } = [];

    /// <summary>Top authors by how many times their threads have been saved, highest first.</summary>
    public IReadOnlyList<LeaderboardEntry> Leaderboard { get; set; } = [];
}