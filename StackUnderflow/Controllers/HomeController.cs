using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Utilities;

namespace StackUnderflow.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    private const int PageSize = 5;

    public async Task<IActionResult> Index(bool requireAllTags = false)
    {
        ViewData["RequireAllTags"] = requireAllTags;
        var totalCount = _context.SUThreads.Count();

        List<SUThread> threads = _context.SUThreads
            .Include(t => t.User)
            .Include(t => t.Posts)
            .Include(t => t.ThreadTags)
            .ThenInclude(tt => tt.Tag)
            // Keep this ordering in sync with the paginated API so "Load More" pages line up.
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(PageSize)
            .ToList();

        ViewData["PageSize"] = PageSize;
        ViewData["CurrentPage"] = 1;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / PageSize);

        return View(new HomeViewModel
        {
            Threads = threads,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(string? query, string[]? tags = null, string? removeTag = null, bool requireAllTags = false)
    {
        var search = new ThreadSearch(query, tags, removeTag, requireAllTags);
        if (search.Query.Length == 0)
        {
            return RedirectToAction("Index", new { requireAllTags });
        }

        var filtered = search.Apply(_context.SUThreads);

        var totalCount = filtered.Count();

        var threads = filtered
            .Include(t => t.User)
            .Include(t => t.Posts)
            .Include(t => t.ThreadTags)
            .ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(PageSize)
            .ToList();

        ViewData["PageSize"] = PageSize;
        ViewData["CurrentPage"] = 1;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / PageSize);

        ViewData["Query"] = search.Query;
        ViewData["SearchText"] = search.Text;
        ViewData["SearchTags"] = search.Tags;
        ViewData["RequireAllTags"] = requireAllTags;

        return View(new HomeViewModel
        {
            Threads = threads,
        });
    }
    private const int LeaderboardPageSize = 5;

    // Ranked users, sorted by one of the leaderboard options (reputation / saves /
    // accepted answers). Every option is computed the same way — a projection over
    // the user — and paged via the query string (?sort=&query=&page=).
    public async Task<IActionResult> Leaderboard(string? sort = null, string? query = null, int page = 1)
    {
        sort = LeaderboardViewModel.NormalizeSort(sort);
        page = Math.Max(1, page);

        var users = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            users = users.Where(u =>
                (u.UserName != null && u.UserName.Contains(query)) ||
                (u.Email != null && u.Email.Contains(query)));
        }

        // Order on the same expressions the row projection uses, with Id as a stable
        // tiebreak so pages never overlap or skip. Reputation is the secondary sort
        // for the count-based options so ties resolve sensibly.
        var ordered = sort switch
        {
            LeaderboardViewModel.Saved => users
                .OrderByDescending(u => u.SUThreads.SelectMany(t => t.SavedBy).Count())
                .ThenByDescending(u => u.Reputation)
                .ThenBy(u => u.Id),
            LeaderboardViewModel.Accepted => users
                .OrderByDescending(u => u.Posts.Count(p => p.IsAcceptedAnswer))
                .ThenByDescending(u => u.Reputation)
                .ThenBy(u => u.Id),
            _ => users
                .OrderByDescending(u => u.Reputation)
                .ThenBy(u => u.Id),
        };

        var totalCount = await users.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / LeaderboardPageSize));

        var rows = await ordered
            .Skip((page - 1) * LeaderboardPageSize)
            .Take(LeaderboardPageSize)
            .Select(u => new LeaderboardRow
            {
                UserId = u.Id,
                Name = u.UserName ?? u.Email ?? "User",
                Reputation = u.Reputation,
                SavedCount = u.SUThreads.SelectMany(t => t.SavedBy).Count(),
                AcceptedAnswerCount = u.Posts.Count(p => p.IsAcceptedAnswer),
            })
            .ToListAsync();

        // Fill in the display name (strip any email domain) + avatar initial + global
        // rank in memory, once the page has been fetched.
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var at = row.Name.IndexOf('@');
            if (at > 0) row.Name = row.Name[..at];
            row.Initials = string.IsNullOrEmpty(row.Name) ? "?" : row.Name[..1].ToUpperInvariant();
            row.Rank = ((page - 1) * LeaderboardPageSize) + i + 1;
        }

        ViewData["PageSize"] = LeaderboardPageSize;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;

        return View(new LeaderboardViewModel
        {
            Rows = rows,
            Sort = sort,
            Query = query,
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
