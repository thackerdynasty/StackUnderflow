using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Controllers;

public class ThreadController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public ThreadController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    [Route("/Thread/{id}")]
    public IActionResult Detail(int id)
    {
        var thread = _context.SUThreads
            .Include(t => t.User)
            .Include(t => t.Posts)
            .ThenInclude(p => p.User)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Comments)
            .ThenInclude(c => c.User)
            .FirstOrDefault(t => t.Id == id);
        
        if (thread == null)
            return NotFound();
        
        return View(thread);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{id}/Answer")]
    public IActionResult PostAnswer(int id, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["AnswerError"] = "Answer content is required.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var threadExists = _context.SUThreads.Any(t => t.Id == id);
        if (!threadExists)
            return NotFound();

        var post = new Post
        {
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Upvotes = 0,
            Downvotes = 0,
            IsAcceptedAnswer = false,
            UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            SUThreadId = id,
            Comments = new List<Comment>()
        };

        _context.Posts.Add(post);
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id });
    }
}
