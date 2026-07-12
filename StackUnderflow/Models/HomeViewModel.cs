namespace StackUnderflow.Models;

/// <summary>
/// Data shown on the home page: the top questions list plus the reputation leaderboard.
/// </summary>
public class HomeViewModel
{
    public IReadOnlyList<SUThread> Threads { get; set; } = [];

    /// <summary>Top users by reputation, highest first, for the leaderboard panel.</summary>
    public IReadOnlyList<User> TopUsers { get; set; } = [];
}