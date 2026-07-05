using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    private const int PageSize = 5;

    public IActionResult Index()
    {
        var totalCount = _context.SUThreads.Count();

        List<SUThread> threads = _context.SUThreads
            .Include(t => t.User)
            .Include(t => t.Posts)
            // Keep this ordering in sync with the paginated API so "Load More" pages line up.
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(PageSize)
            .ToList();

        ViewData["PageSize"] = PageSize;
        ViewData["CurrentPage"] = 1;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / PageSize);

        return View(threads);
    }

    [HttpPost]
    public IActionResult Index(string query)
    {
        var threads = _context.SUThreads
            .Where(t => t.Title.Contains(query) || t.Content.Contains(query))
            .Include(t => t.User)
            .Include(t => t.Posts)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(PageSize)
            .ToList();
        
        ViewData["PageSize"] = PageSize;
        ViewData["CurrentPage"] = 1;
        ViewData["TotalPages"] = (int)Math.Ceiling((double)threads.Count / PageSize);
        
        ViewData["Query"] = query;
        
        return View(threads);
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
