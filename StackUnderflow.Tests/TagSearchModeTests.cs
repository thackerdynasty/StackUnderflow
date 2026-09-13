using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Controllers;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;

namespace StackUnderflow.Tests;

public sealed class TagSearchModeTests
{
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    public async Task Index_AndPaginationMatchSelectedTagMode(bool requireAllTags, int expectedCount)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var author = new User
        {
            Id = "author", UserName = "author", Bio = "",
            ProfilePicture = new Uri("https://example.com/profile.png")
        };
        var firstTag = new Tag { Name = "csharp" };
        var secondTag = new Tag { Name = "dotnet" };
        foreach (var tags in new Tag[][] { [firstTag], [secondTag], [firstTag, secondTag], [] })
        {
            context.SUThreads.Add(new SUThread
            {
                Title = "Collections", User = author,
                ThreadTags = tags.Select(tag => new ThreadTag { Tag = tag }).ToList()
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var home = new HomeController(context, new FakeProfileImageStorage());
        var result = Assert.IsType<ViewResult>(await home.Index("Collections #csharp #dotnet #csharp", requireAllTags: requireAllTags));
        Assert.Equal(expectedCount, Assert.IsType<HomeViewModel>(result.Model).Threads.Count);
        Assert.Equal(requireAllTags, home.ViewData["RequireAllTags"]);

        var api = new StackUnderflow.Areas.Api.ThreadController(context, new ThreadVoteService(context));
        var response = await api.GetSUThreadsPaginated(new PaginationRequest { PageSize = 1 },
            (string?)home.ViewData["Query"], requireAllTags: requireAllTags);
        var page = Assert.IsType<PaginatedResponse<ThreadSummaryDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(expectedCount, page.TotalCount);
        Assert.Equal(expectedCount, page.TotalPages);

        result = Assert.IsType<ViewResult>(await home.Index("Collections", ["csharp", "dotnet"], "dotnet", requireAllTags));
        Assert.Equal(2, Assert.IsType<HomeViewModel>(result.Model).Threads.Count);
        Assert.Equal(requireAllTags, home.ViewData["RequireAllTags"]);

        result = Assert.IsType<ViewResult>(await home.Index("Collections", requireAllTags: requireAllTags));
        Assert.Equal(4, Assert.IsType<HomeViewModel>(result.Model).Threads.Count);

        var redirect = Assert.IsType<RedirectToActionResult>(await home.Index("", ["csharp"], "csharp", requireAllTags));
        Assert.Equal(requireAllTags, redirect.RouteValues!["requireAllTags"]);
    }
}
