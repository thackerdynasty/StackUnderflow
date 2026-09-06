using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;
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

        var leaderboard = await LeaderboardService.GetTopAuthorsAsync(_context, 3);

        return View(new HomeViewModel
        {
            Threads = threads,
            Leaderboard = leaderboard
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

        var leaderboard = await LeaderboardService.GetTopAuthorsAsync(_context, 3);

        return View(new HomeViewModel
        {
            Threads = threads,
            Leaderboard = leaderboard
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
