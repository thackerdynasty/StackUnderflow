using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StackUnderflow.Controllers;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;
using StackUnderflow.Utilities;

namespace StackUnderflow.Tests;

public sealed class ThreadReportingTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ThreadReportingTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public void Report_AddsPendingReportForAnotherUsersThread()
    {
        using var context = CreateContext();
        var reporter = CreateUser("reporter");
        var thread = CreateThread(CreateUser("author"));
        context.AddRange(reporter, thread);
        context.SaveChanges();

        var controller = CreateThreadController(context, reporter.Id);

        var result = controller.Report(thread.Id, new ThreadReportViewModel
        {
            Reason = ThreadReportReasons.Spam,
            Details = "This appears to be promotional content."
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ThreadController.Detail), redirect.ActionName);

        var report = Assert.Single(context.ThreadReports);
        Assert.Equal(reporter.Id, report.ReporterId);
        Assert.Equal(thread.Id, report.SUThreadId);
        Assert.Equal(ThreadReportReasons.Spam, report.Reason);
        Assert.Null(report.ReviewedAt);
        Assert.Null(report.ReviewedById);
        Assert.Null(report.Resolution);
    }

    [Fact]
    public void Report_RejectsDuplicateReportFromSameUser()
    {
        using var context = CreateContext();
        var reporter = CreateUser("reporter");
        var thread = CreateThread(CreateUser("author"));
        context.AddRange(reporter, thread);
        context.ThreadReports.Add(new ThreadReport
        {
            Reason = ThreadReportReasons.Spam,
            ReportedAt = DateTime.UtcNow,
            Reporter = reporter,
            SUThread = thread
        });
        context.SaveChanges();

        var controller = CreateThreadController(context, reporter.Id);

        var result = controller.Report(thread.Id, new ThreadReportViewModel
        {
            Reason = ThreadReportReasons.Harassment
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Single(context.ThreadReports);
    }

    [Fact]
    public void MarkSafe_ClosesAllPendingReportsForTheThread()
    {
        using var context = CreateContext();
        var reporter = CreateUser("reporter");
        var secondReporter = CreateUser("second-reporter");
        var moderator = CreateUser("moderator", isModerator: true);
        var otherModerator = CreateUser("other-moderator", isModerator: true);
        var thread = CreateThread(CreateUser("author"));
        var report = new ThreadReport
        {
            Reason = ThreadReportReasons.InappropriateContent,
            ReportedAt = DateTime.UtcNow,
            Reporter = reporter,
            SUThread = thread
        };
        var secondReport = new ThreadReport
        {
            Reason = ThreadReportReasons.Spam,
            ReportedAt = DateTime.UtcNow,
            Reporter = secondReporter,
            SUThread = thread
        };
        context.AddRange(moderator, otherModerator, report, secondReport);
        context.SaveChanges();

        var reviewingController = CreateModerationController(context, otherModerator.Id);

        var result = reviewingController.CompleteReview(
            report.Id,
            ThreadReportResolution.Safe,
            "Reviewed; no action required.");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ModerationController.Index), redirect.ActionName);

        context.ChangeTracker.Clear();
        var reviewedReports = context.ThreadReports.OrderBy(r => r.Id).ToList();
        Assert.Equal(2, reviewedReports.Count);
        Assert.All(reviewedReports, reviewedReport =>
        {
            Assert.Equal(otherModerator.Id, reviewedReport.ReviewedById);
            Assert.NotNull(reviewedReport.ReviewedAt);
            Assert.Equal(ThreadReportResolution.Safe, reviewedReport.Resolution);
            Assert.Equal("Reviewed; no action required.", reviewedReport.ModeratorNotes);
        });

        var otherModeratorDashboard = CreateModerationController(context, moderator.Id);
        var dashboardResult = Assert.IsType<ViewResult>(otherModeratorDashboard.Index());
        var pendingReports = Assert.IsAssignableFrom<IReadOnlyList<ThreadReport>>(dashboardResult.Model);
        Assert.Empty(pendingReports);
    }

    [Fact]
    public void MarkUnsafe_DeletesThreadAndAllDependentContent()
    {
        using var context = CreateContext();
        var reporter = CreateUser("reporter");
        var moderator = CreateUser("moderator", isModerator: true);
        var answerAuthor = CreateUser("answer-author");
        var thread = CreateThread(CreateUser("author"));
        var post = new Post
        {
            Content = "An answer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = answerAuthor,
            SUThread = thread
        };
        var report = new ThreadReport
        {
            Reason = ThreadReportReasons.InappropriateContent,
            ReportedAt = DateTime.UtcNow,
            Reporter = reporter,
            SUThread = thread
        };
        var comment = new Comment
        {
            Content = "A comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = reporter,
            Post = post
        };
        var savedThread = new SavedThread
        {
            SavedAt = DateTime.UtcNow,
            User = reporter,
            SUThread = thread
        };
        var threadVote = new ThreadVote
        {
            Value = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = reporter,
            SUThread = thread
        };
        var postVote = new PostVote
        {
            Value = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = reporter,
            Post = post
        };
        var tag = new Tag { Name = "reported" };
        var threadTag = new ThreadTag { Tag = tag, SUThread = thread };
        context.AddRange(moderator, report, comment, savedThread, threadVote, postVote, threadTag);
        context.SaveChanges();

        var controller = CreateModerationController(context, moderator.Id);

        var result = controller.CompleteReview(
            report.Id,
            ThreadReportResolution.Unsafe,
            "Confirmed unsafe content.");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ModerationController.Index), redirect.ActionName);

        context.ChangeTracker.Clear();
        Assert.Empty(context.SUThreads);
        Assert.Empty(context.Posts);
        Assert.Empty(context.Comments);
        Assert.Empty(context.ThreadReports);
        Assert.Empty(context.SavedThreads);
        Assert.Empty(context.ThreadVotes);
        Assert.Empty(context.PostVotes);
        Assert.Empty(context.ThreadTags);
        Assert.Single(context.Tags);
    }

    [Fact]
    public async Task ToggleLock_AllowsModeratorWhoDoesNotOwnThread()
    {
        using var context = CreateContext();
        var moderator = CreateUser("moderator", isModerator: true);
        var thread = CreateThread(CreateUser("author"));
        context.AddRange(moderator, thread);
        context.SaveChanges();

        var controller = CreateThreadController(context, moderator.Id);

        var result = await controller.ToggleLock(thread.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ThreadController.Detail), redirect.ActionName);
        Assert.True(thread.IsLocked);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ThreadController CreateThreadController(ApplicationDbContext context, string userId)
    {
        var httpContext = CreateHttpContext(userId);
        var contentSafetyAnalyzer = new ContentSafetyAnalyzer(
            new ConfigurationBuilder().Build(),
            NullLogger<ContentSafetyAnalyzer>.Instance);
        var controller = new ThreadController(
            context,
            new ThreadVoteService(context),
            new PostVoteService(context),
            contentSafetyAnalyzer)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        return controller;
    }

    private static ModerationController CreateModerationController(ApplicationDbContext context, string userId)
    {
        var httpContext = CreateHttpContext(userId);
        return new ModerationController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static DefaultHttpContext CreateHttpContext(string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "Test");

        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private static User CreateUser(string id, bool isModerator = false)
    {
        return new User
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            Email = $"{id}@example.com",
            NormalizedEmail = $"{id}@example.com".ToUpperInvariant(),
            EmailConfirmed = true,
            Bio = "",
            ProfilePicture = new Uri("https://example.com/profile.png"),
            JoinDate = DateTime.UtcNow,
            IsModerator = isModerator
        };
    }

    private static SUThread CreateThread(User author)
    {
        return new SUThread
        {
            Title = "Reported thread",
            Content = "Thread content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = author,
            UserId = author.Id
        };
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
