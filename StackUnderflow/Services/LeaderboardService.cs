using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Services;

/// <summary>
/// Builds the home-page leaderboard, ranking users by how many times their
/// authored threads have been saved by other users ("most-saved authors").
/// Shared by <c>HomeController</c> (initial render) and the saved-threads API
/// (live refresh) so both return the same ordering.
/// </summary>
public static class LeaderboardService
{
    public static async Task<List<LeaderboardEntry>> GetTopAuthorsAsync(ApplicationDbContext context, int count)
    {
        // Count saves grouped by the author of the saved thread, highest first.
        var ranked = await context.SavedThreads
            .GroupBy(s => s.SUThread.UserId)
            .Select(g => new { UserId = g.Key, SaveCount = g.Count() })
            .OrderByDescending(x => x.SaveCount)
            .Take(count)
            .ToListAsync();

        if (ranked.Count == 0)
            return [];

        var userIds = ranked.Select(r => r.UserId).ToList();
        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToDictionaryAsync(u => u.Id);

        // Preserve the ranked order; drop any author whose account no longer exists.
        return ranked
            .Where(r => users.ContainsKey(r.UserId))
            .Select(r =>
            {
                var user = users[r.UserId];
                var name = DisplayName(user.UserName ?? user.Email ?? "User");
                return new LeaderboardEntry
                {
                    UserId = r.UserId,
                    Name = name,
                    Initials = string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant(),
                    SaveCount = r.SaveCount
                };
            })
            .ToList();
    }

    // Mirrors the username-to-display-name convention used elsewhere (strip email domain).
    private static string DisplayName(string name)
    {
        var at = name.IndexOf('@');
        return at > 0 ? name[..at] : name;
    }
}