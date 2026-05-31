using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using System.Security.Claims;

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

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                ViewBag.QuestionVote = _context.ThreadVotes
                    .Where(v => v.UserId == userId && v.SUThreadId == id)
                    .Select(v => v.Value)
                    .FirstOrDefault();

                ViewBag.AnswerVotes = _context.PostVotes
                    .Where(v => v.UserId == userId && v.Post.SUThreadId == id)
                    .ToDictionary(v => v.PostId, v => v.Value);
            }
        }
        
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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{id}/Post/{postId}/Comment")]
    public IActionResult PostComment(int id, int postId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["CommentError"] = "Comment content is required.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var comment = new Comment
        {
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PostId = postId,
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        };
        
        _context.Comments.Add(comment);
        _context.SaveChanges();
        
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{id}/Vote")]
    public IActionResult VoteQuestion(int id, string vote)
    {
        var voteValue = ParseVoteValue(vote);
        if (voteValue == null)
            return BadRequest();

        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var existingVote = _context.ThreadVotes.FirstOrDefault(v => v.UserId == userId && v.SUThreadId == id);

        if (existingVote == null)
        {
            ApplyThreadVote(thread, voteValue.Value);
            _context.ThreadVotes.Add(new ThreadVote
            {
                Value = voteValue.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId,
                SUThreadId = id
            });
        }
        else if (existingVote.Value != voteValue.Value)
        {
            RevertThreadVote(thread, existingVote.Value);
            ApplyThreadVote(thread, voteValue.Value);
            existingVote.Value = voteValue.Value;
            existingVote.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            RevertThreadVote(thread, existingVote.Value);
            _context.ThreadVotes.Remove(existingVote);
        }

        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Answer/{postId}/Vote")]
    public IActionResult VoteAnswer(int threadId, int postId, string vote)
    {
        var voteValue = ParseVoteValue(vote);
        if (voteValue == null)
            return BadRequest();

        var post = _context.Posts.FirstOrDefault(p => p.Id == postId && p.SUThreadId == threadId);
        if (post == null)
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var existingVote = _context.PostVotes.FirstOrDefault(v => v.UserId == userId && v.PostId == postId);

        if (existingVote == null)
        {
            ApplyPostVote(post, voteValue.Value);
            _context.PostVotes.Add(new PostVote
            {
                Value = voteValue.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId,
                PostId = postId
            });
        }
        else if (existingVote.Value != voteValue.Value)
        {
            RevertPostVote(post, existingVote.Value);
            ApplyPostVote(post, voteValue.Value);
            existingVote.Value = voteValue.Value;
            existingVote.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            RevertPostVote(post, existingVote.Value);
            _context.PostVotes.Remove(existingVote);
        }

        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id = threadId });
    }

    private static int? ParseVoteValue(string vote)
    {
        return vote switch
        {
            "up" => 1,
            "down" => -1,
            _ => null
        };
    }

    private static void ApplyThreadVote(SUThread thread, int voteValue)
    {
        if (voteValue > 0)
            thread.UpvoteCount++;
        else
            thread.DownvoteCount++;
    }

    private static void RevertThreadVote(SUThread thread, int voteValue)
    {
        if (voteValue > 0)
            thread.UpvoteCount = Math.Max(0, thread.UpvoteCount - 1);
        else
            thread.DownvoteCount = Math.Max(0, thread.DownvoteCount - 1);
    }

    private static void ApplyPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
            post.Upvotes++;
        else
            post.Downvotes++;
    }

    private static void RevertPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
            post.Upvotes = Math.Max(0, post.Upvotes - 1);
        else
            post.Downvotes = Math.Max(0, post.Downvotes - 1);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Accept/{postId}")]
    public IActionResult AcceptAnswer(int id, int postId)
    {
        var thread = _context.SUThreads.Find(id);
        var post =  _context.Posts.Find(postId);
        if (thread == null || post == null) return NotFound();
        thread.IsSolved = true;
        post.IsAcceptedAnswer = true;
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id });
    }
}
