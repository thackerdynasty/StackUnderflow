using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Controllers;

// Whole controller requires an authenticated user. Unauthenticated requests are
// redirected to the Identity login page by the authorization middleware.
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public ProfileController(UserManager<User> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    // GET: /Profile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildProfileAsync(user, isOwnProfile: true);
        return View(model);
    }

    // GET: /Profile/Details/{id} — public profile for any user, linked from usernames on threads.
    [AllowAnonymous]
    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // If the viewer is looking at their own profile, send them to the editable Index view.
        if (User.Identity?.IsAuthenticated == true && id == _userManager.GetUserId(User))
        {
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildProfileAsync(user, isOwnProfile: false);
        return View(nameof(Index), model);
    }

    private async Task<ProfileViewModel> BuildProfileAsync(User user, bool isOwnProfile)
    {
        var questions = await _dbContext.SUThreads
            .AsNoTracking()
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var answers = await _dbContext.Posts
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Include(p => p.SUThread)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .Include(c => c.Post)
            .ThenInclude(p => p.SUThread)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return new ProfileViewModel
        {
            User = user,
            IsOwnProfile = isOwnProfile,
            Questions = questions,
            Answers = answers,
            Comments = comments,
            QuestionCount = questions.Count,
            AnswerCount = answers.Count,
            AcceptedAnswerCount = answers.Count(p => p.IsAcceptedAnswer),
            CommentCount = comments.Count,
        };
    }

    // POST: /Profile/UpdateBio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBio(string? bio)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.Bio = bio?.Trim() ?? string.Empty;
        var result = await _userManager.UpdateAsync(user);

        TempData["ProfileStatus"] = result.Succeeded
            ? "Your bio has been updated."
            : "Could not update your bio. Please try again.";

        return RedirectToAction(nameof(Index));
    }
}