namespace StackUnderflow.Models;

/// <summary>
/// One ranked user on the leaderboard page. Every sort option is computed the
/// same way (a projection over the user), so a single row carries the numbers
/// for all of them and the view just emphasises whichever one is active.
/// </summary>
public class LeaderboardRow
{
    public int Rank { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Single-letter avatar initial derived from <see cref="Name"/>.</summary>
    public string Initials { get; set; } = "?";

    public int Reputation { get; set; }

    /// <summary>Number of times this user's threads have been saved by others.</summary>
    public int SavedCount { get; set; }

    /// <summary>Number of this user's answers that were accepted.</summary>
    public int AcceptedAnswerCount { get; set; }
}

/// <summary>
/// A single leaderboard sort option: the query-string key, the tab label, and
/// how to describe its value on each row. Shared by the controller (to validate
/// and order) and the view (to render tabs + the active stat).
/// </summary>
public record LeaderboardSortOption(string Key, string Label, string UnitSingular, string UnitPlural);

/// <summary>Data for the leaderboard page: the ranked rows plus the current sort/search state.</summary>
public class LeaderboardViewModel
{
    public IReadOnlyList<LeaderboardRow> Rows { get; set; } = [];

    /// <summary>The active sort key (one of <see cref="Options"/>).</summary>
    public string Sort { get; set; } = Reputation;

    /// <summary>The current search term, echoed back into the search box.</summary>
    public string? Query { get; set; }

    public const string Reputation = "reputation";
    public const string Saved = "saved";
    public const string Accepted = "accepted";

    /// <summary>All sort options, in tab order. First entry is the default.</summary>
    public static readonly IReadOnlyList<LeaderboardSortOption> Options =
    [
        new(Reputation, "Highest Reputation", "reputation", "reputation"),
        new(Saved, "Most Saved Threads", "saved thread", "saved threads"),
        new(Accepted, "Most Accepted Answers", "accepted answer", "accepted answers"),
    ];

    /// <summary>Normalise an incoming sort value to a known key (defaults to reputation).</summary>
    public static string NormalizeSort(string? sort) =>
        Options.Any(o => o.Key == sort) ? sort! : Reputation;
}
