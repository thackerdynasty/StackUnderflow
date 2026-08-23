using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using System.Security.Claims;

namespace StackUnderflow.Controllers;

[Authorize(Policy = "IsModerator")]
public class ModerationController(ApplicationDbContext context) : Controller
{
    public IActionResult Index()
    {
        var reports = context.ThreadReports
            .AsNoTracking()
            .Where(r => r.ReviewedAt == null)
            .Include(r => r.SUThread)
            .Include(r => r.Reporter)
            .OrderBy(r => r.ReportedAt)
            .ToList();

        return View(reports);
    }

    [HttpGet]
    public IActionResult Review(int id)
    {
        var report = context.ThreadReports
            .AsNoTracking()
            .Include(r => r.SUThread)
            .ThenInclude(t => t.User)
            .Include(r => r.Reporter)
            .Include(r => r.ReviewedBy)
            .FirstOrDefault(r => r.Id == id);

        return report == null ? NotFound() : View(report);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteReview(int id, ThreadReportResolution decision, string? moderatorNotes)
    {
        var report = context.ThreadReports.FirstOrDefault(r => r.Id == id);
        if (report == null)
            return NotFound();

        if (report.ReviewedAt != null)
        {
            TempData["ModerationInfo"] = "This report has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        if (moderatorNotes?.Length > 1000)
        {
            TempData["ModerationError"] = "Moderator notes cannot exceed 1000 characters.";
            return RedirectToAction(nameof(Review), new { id });
        }

        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var notes = string.IsNullOrWhiteSpace(moderatorNotes) ? null : moderatorNotes.Trim();

        if (decision == ThreadReportResolution.Safe)
        {
            var reviewedAt = DateTime.UtcNow;
            var pendingReports = context.ThreadReports
                .Where(r => r.SUThreadId == report.SUThreadId && r.ReviewedAt == null)
                .ToList();

            foreach (var pendingReport in pendingReports)
            {
                pendingReport.ReviewedAt = reviewedAt;
                pendingReport.ReviewedById = moderatorId;
                pendingReport.ModeratorNotes = notes;
                pendingReport.Resolution = ThreadReportResolution.Safe;
            }

            context.SaveChanges();
            TempData["ModerationSuccess"] = "The thread was marked safe and all of its pending reports were closed.";
            return RedirectToAction(nameof(Index));
        }

        if (decision == ThreadReportResolution.Unsafe)
        {
            DeleteThread(report.SUThreadId);
            context.SaveChanges();
            TempData["ModerationSuccess"] = "The unsafe thread and its related content were deleted.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ModerationError"] = "Choose whether the thread is safe or unsafe.";
        return RedirectToAction(nameof(Review), new { id });
    }

    private void DeleteThread(int threadId)
    {
        var thread = context.SUThreads.First(t => t.Id == threadId);
        var postIds = context.Posts
            .Where(p => p.SUThreadId == threadId)
            .Select(p => p.Id)
            .ToList();

        context.Comments.RemoveRange(context.Comments.Where(c => postIds.Contains(c.PostId)));
        context.PostVotes.RemoveRange(context.PostVotes.Where(v => postIds.Contains(v.PostId)));
        context.Posts.RemoveRange(context.Posts.Where(p => p.SUThreadId == threadId));
        context.SavedThreads.RemoveRange(context.SavedThreads.Where(s => s.SUThreadId == threadId));
        context.ThreadVotes.RemoveRange(context.ThreadVotes.Where(v => v.SUThreadId == threadId));
        context.ThreadTags.RemoveRange(context.ThreadTags.Where(t => t.SUThreadId == threadId));
        context.ThreadReports.RemoveRange(context.ThreadReports.Where(r => r.SUThreadId == threadId));
        context.SUThreads.Remove(thread);
    }
}
