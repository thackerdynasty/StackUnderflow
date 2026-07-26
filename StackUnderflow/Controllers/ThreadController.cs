using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace StackUnderflow.Controllers;

public partial class ThreadController(ApplicationDbContext context) : Controller
{

    private readonly ApplicationDbContext _context = context;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9+#.\-]{0,24}$")]
    private static partial Regex TagNameRegex();

    private const int MaxTags = 5;

    private string? ApplyTags(SUThread thread, string tags)
    {
        var submitted = (tags ?? "")
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(n => n.ToLowerInvariant())
            .Distinct()
            .ToList();

        var valid = submitted.Where(n => TagNameRegex().IsMatch(n)).ToList();
        var rejectedCount = submitted.Count - valid.Count;

        foreach (var name in valid.Take(MaxTags))
        {
            var tag = _context.Tags.FirstOrDefault(t => t.Name == name)
                    ?? new Tag { Name = name };
            thread.ThreadTags.Add(new ThreadTag { Tag = tag });
        }

        var warnings = new List<string>();
        if (rejectedCount > 0)
            warnings.Add($"{rejectedCount} tag{(rejectedCount == 1 ? " was" : "s were")} ignored: tags may only contain lowercase letters, numbers, and + # . - (max 25 characters).");
        if (valid.Count > MaxTags)
            warnings.Add($"Only the first {MaxTags} tags were kept.");

        return warnings.Count > 0 ? string.Join(" ", warnings) : null;
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
    public IActionResult Create(string title, string content, string threadTags)
    {
        System.Diagnostics.Debug.WriteLine($"Creating thread with title: {title}, content: {content}, tags: {threadTags}");
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
            Posts = new List<Post>(),
        };
        
        TempData["TagWarning"] = ApplyTags(thread, threadTags);
        _context.SUThreads.Add(thread);
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id = thread.Id });
    }

    [Route("/Thread/{id}/Edit")]
    public IActionResult Edit(int id)
    {
        var thread = _context.SUThreads
            .Include(t => t.ThreadTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();
        return View(thread);
    }

    [Authorize]
    [HttpPost]
    [Route("/Thread/{id}/Edit")]
    public IActionResult Edit(int id, string title, string content, string threadTags)
    {
        var thread = _context.SUThreads
            .Include(t => t.ThreadTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefault(t => t.Id == id);
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
        thread.ThreadTags.Clear();
        TempData["TagWarning"] = ApplyTags(thread, threadTags);

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
        var thread = _context.SUThreads.FirstOrDefault(t => t.Id == id);
        if (thread == null)
            return NotFound();
        if (thread.UserId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return Forbid();
        _context.SUThreads.Remove(thread);
        _context.SaveChanges();
        return Redirect("/");
    }

    private const int AnswersPageSize = 5;
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
            .Include(t => t.Posts)
            .ThenInclude(p => p.User)
            .Include(t => t.Posts)
            .ThenInclude(p => p.Comments)
            .ThenInclude(c => c.User)
            .Include(t => t.ThreadTags)
            .ThenInclude(tt => tt.Tag)
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
    public IActionResult VoteQuestion(int id, string vote)
    {
        var voteValue = ParseVoteValue(vote);
        if (voteValue == null)
            return BadRequest();

        var thread = _context.SUThreads
            .Include(t => t.User)
            .FirstOrDefault(t => t.Id == id);
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

        var post = _context.Posts
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == postId && p.SUThreadId == threadId);
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
        {
            thread.UpvoteCount++;
            User user = thread.User;
            user.Reputation += 10;
        }
        else
        {
            thread.DownvoteCount++;
            User user = thread.User;
            user.Reputation -= 2;
        }
    }

    private static void RevertThreadVote(SUThread thread, int voteValue)
    {
        if (voteValue > 0)
        {
            thread.UpvoteCount = Math.Max(0, thread.UpvoteCount - 1);
            thread.User.Reputation -= 10;
        }
        else
        {
            thread.DownvoteCount = Math.Max(0, thread.DownvoteCount - 1);
            thread.User.Reputation += 2;
        }
    }

    private static void ApplyPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
        {
            post.Upvotes++;
            post.User.Reputation += 10;
        }
        else
        {
            post.Downvotes++;
            post.User.Reputation -= 2;
        }
    }

    private static void RevertPostVote(Post post, int voteValue)
    {
        if (voteValue > 0)
        {
            post.Upvotes = Math.Max(0, post.Upvotes - 1);
            post.User.Reputation -= 10;
        }
        else
        {
            post.Downvotes = Math.Max(0, post.Downvotes - 1);
            post.User.Reputation += 2;
        }
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
        post.User.Reputation += 15;
        thread.User.Reputation += 2;
        _context.SaveChanges();
        return RedirectToAction(nameof(Detail), new { id });
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9+#.\-]{0,24}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
