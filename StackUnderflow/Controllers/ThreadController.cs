using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;
using System.Security.Claims;

namespace StackUnderflow.Controllers;

public class ThreadController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ThreadVoteService _voteService;
    private readonly PostVoteService _postVoteService;

    public ThreadController(ApplicationDbContext context, ThreadVoteService voteService, PostVoteService postVoteService)
    {
        _context = context;
        _voteService = voteService;
        _postVoteService = postVoteService;
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }

    [Route("/Thread/Create")]
    public IActionResult Create()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [Route("/Thread/Create")]
    public IActionResult Create(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Create));
        }
        var thread = new SUThread
        {
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpvoteCount = 0,
            DownvoteCount = 0,
            ViewCount = 0,
            IsSolved = false,
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value,
            Posts = new List<Post>()
        };
        _context.SUThreads.Add(thread);
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id = thread.Id });
    }

    [Route("/Thread/{id}/Edit")]
    public IActionResult Edit(int id)
    {
        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();
        return View(thread);
    }

    [Authorize]
    [HttpPost]
    [Route("/Thread/{id}/Edit")]
    public IActionResult Edit(int id, string title, string content)
    {
        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        thread.Title = title.Trim();
        thread.Content = content.Trim();
        thread.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Route("/Thread/{id}/Delete")]
    public IActionResult Delete(int id)
    {
        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();
        return View(thread);
    }

    [Authorize]
    [HttpPost]
    [Route("/Thread/{id}/Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        var thread = _context.SUThreads
            .Include(t => t.Posts)
                .ThenInclude(p => p.Comments)
            .Include(t => t.Posts)
                .ThenInclude(p => p.Votes)
            .Include(t => t.SavedBy)
            .FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();

        // Posts and SavedThreads use NoAction on the thread FK, so their rows must
        // be removed explicitly before the thread. ThreadVotes cascade automatically.
        foreach (var post in thread.Posts)
        {
            _context.Comments.RemoveRange(post.Comments);
            _context.PostVotes.RemoveRange(post.Votes);
        }
        _context.Posts.RemoveRange(thread.Posts);
        _context.SavedThreads.RemoveRange(thread.SavedBy);
        _context.SUThreads.Remove(thread);
        _context.SaveChanges();
        return Redirect("/");
    }

    private const int AnswersPageSize = 5;

    // Answers ordered for display: accepted first, then by score, then oldest.
    // Id is a stable tiebreak so paged slices don't overlap or skip.
    private IQueryable<Post> OrderedAnswers(int threadId) =>
        _context.Posts
            .Where(p => p.SUThreadId == threadId)
            .OrderByDescending(p => p.IsAcceptedAnswer)
            .ThenByDescending(p => p.Upvotes - p.Downvotes)
            .ThenBy(p => p.CreatedAt)
            .ThenBy(p => p.Id);

    [Route("/Thread/{id}")]
    public IActionResult Detail(int id)
    {
        var thread = _context.SUThreads
            .Include(t => t.User)
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

                ViewBag.IsSaved = _context.SavedThreads
                    .Any(s => s.UserId == userId && s.SUThreadId == id);
            }

            if (userId != thread.UserId)
            {
                thread.ViewCount++;
            }
        }
        else
        {
            thread.ViewCount++;
        }

        _context.SaveChanges();

        // Load only the first page of answers; the rest arrive via the Answers endpoint.
        ViewBag.TotalAnswers = _context.Posts.Count(p => p.SUThreadId == id);
        ViewBag.AnswersPageSize = AnswersPageSize;
        ViewBag.AnswersCurrentPage = 1;

        thread.Posts = OrderedAnswers(id)
            .Include(p => p.User)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .Take(AnswersPageSize)
            .ToList();

        return View(thread);
    }

    // Returns a rendered partial with one page of answers for the "Load more answers" button.
    [Route("/Thread/{id}/Answers")]
    public IActionResult Answers(int id, int page = 1, int pageSize = AnswersPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = AnswersPageSize;

        var thread = _context.SUThreads.AsNoTracking().FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();

        var posts = OrderedAnswers(id)
            .Include(p => p.User)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var answerVotes = new Dictionary<int, int>();
        if (currentUserId != null)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            answerVotes = _context.PostVotes
                .Where(v => v.UserId == currentUserId && postIds.Contains(v.PostId))
                .ToDictionary(v => v.PostId, v => v.Value);
        }

        var isThreadOwner = currentUserId != null && thread.UserId == currentUserId;

        var answers = posts.Select(post => new AnswerViewModel
        {
            Post = post,
            ThreadId = id,
            ThreadUserId = thread.UserId,
            ThreadIsSolved = thread.IsSolved,
            IsThreadOwner = isThreadOwner,
            CurrentUserId = currentUserId,
            AnswerVote = answerVotes.TryGetValue(post.Id, out var vote) ? vote : 0,
            EditPostId = null,
            EditCommentId = null
        }).ToList();

        return PartialView("_AnswerList", answers);
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
            Comments = []
        };

        _context.Posts.Add(post);
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Answer/{postId}/Delete")]
    public IActionResult DeleteAnswer(int threadId, int postId)
    {
        var post = _context.Posts
            .Include(p => p.Comments)
            .Include(p => p.Votes)
            .FirstOrDefault(p => p.Id == postId && p.SUThreadId == threadId);
        if (post == null)
            return NotFound();

        if (post.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();

        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == threadId);
        if (thread == null)
            return NotFound();

        if (post.IsAcceptedAnswer)
        {
            thread.IsSolved = false;
        }

        _context.Comments.RemoveRange(post.Comments);
        _context.PostVotes.RemoveRange(post.Votes);
        _context.Posts.Remove(post);
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id = threadId });
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

        var postExists = _context.Posts.Any(p => p.Id == postId && p.SUThreadId == id);
        if (!postExists)
            return NotFound();

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
    [Route("/Thread/{threadId}/Comment/{commentId}/Edit")]
    public IActionResult EditComment(int threadId, int commentId, string content)
    {
        var comment = _context.Comments
            .Include(c => c.Post)
            .FirstOrDefault(c => c.Id == commentId && c.Post.SUThreadId == threadId);
        if (comment == null)
            return NotFound();

        if (comment.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["CommentEditError"] = "Comment content is required.";
            return RedirectToAction(nameof(Detail), new { id = threadId, editCommentId = commentId });
        }

        comment.Content = content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), null, new { id = threadId }, $"comment-{commentId}");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Comment/{commentId}/Delete")]
    public IActionResult DeleteComment(int threadId, int commentId)
    {
        var comment = _context.Comments
            .Include(c => c.Post)
            .FirstOrDefault(c => c.Id == commentId && c.Post.SUThreadId == threadId);
        if (comment == null)
            return NotFound();

        if (comment.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();

        var postId = comment.PostId;
        _context.Comments.Remove(comment);
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), null, new { id = threadId }, $"answer-{postId}");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{id}/Vote")]
    public async Task<IActionResult> VoteQuestion(int id, string vote)
    {
        var voteValue = ThreadVoteService.ParseVoteValue(vote);
        if (voteValue == null)
            return BadRequest();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var outcome = await _voteService.VoteAsync(id, userId, voteValue.Value);
        if (outcome.Status == ThreadVoteStatus.ThreadNotFound)
            return NotFound();

        // Self-votes are blocked in the UI (buttons are disabled), so we just
        // redirect back rather than surfacing an error page.
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Answer/{postId}/Vote")]
    public async Task<IActionResult> VoteAnswer(int threadId, int postId, string vote)
    {
        var voteValue = PostVoteService.ParseVoteValue(vote);
        if (voteValue == null)
            return BadRequest();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var outcome = await _postVoteService.VoteAsync(postId, userId, voteValue.Value);
        if (outcome.Status == PostVoteStatus.PostNotFound)
            return NotFound();

        // Self-votes are surfaced in the UI; here we just redirect back.
        return RedirectToAction(nameof(Detail), new { id = threadId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("/Thread/{threadId}/Answer/{postId}/Edit")]
    public IActionResult EditAnswer(int threadId, int postId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["PostEditError"] = "Answer content is required.";
            return RedirectToAction(nameof(Detail), new { id = threadId, editPostId = postId });
        }

        var post = _context.Posts.FirstOrDefault(p => p.Id == postId && p.SUThreadId == threadId);
        if (post == null)
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        if (post.UserId != userId)
            return Forbid();

        post.Content = content.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        return RedirectToAction(nameof(Detail), new { id = threadId });
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptAnswer(int id, int postId)
    {
        var thread = _context.SUThreads
            .Include(t => t.User)
            .FirstOrDefault(t => t.Id == id);
        var post =  _context.Posts
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == postId);
        if (thread == null || post == null) return NotFound();
        thread.IsSolved = true;
        post.IsAcceptedAnswer = true;
        if (post.UserId != thread.UserId)
        {
            post.User.Reputation += 15;
            thread.User.Reputation += 2;
        }
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UnacceptAnswer(int id, int postId)
    {
        var thread = _context.SUThreads
            .Include(t => t.User)
            .FirstOrDefault(t => t.Id == id);
        var post =  _context.Posts
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == postId);
        if (thread == null || post == null) return NotFound();
        thread.IsSolved = false;
        post.IsAcceptedAnswer = false;
        if (post.UserId != thread.UserId)
        {
            post.User.Reputation -= 15;
            thread.User.Reputation -= 2;
        }
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id });
    }
}
