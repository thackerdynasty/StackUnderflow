using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Controllers;
using StackUnderflow.Data;
using StackUnderflow.Models;

namespace StackUnderflow.Tests;

public sealed class HomeControllerTests
{
    [Theory]
    [InlineData("#csharp", "Collections")]
    [InlineData("#csharp #dotnet", "Collections", "Database")]
    [InlineData("#csharp #csharp", "Collections")]
    [InlineData("#csharp Collections", "Collections")]
    [InlineData("Collections #csharp", "Collections")]
    [InlineData("Collections #dotnet")]
    [InlineData("Collections", "Collections", "Collections without tags")]
    [InlineData("enumeration", "Collections")]
    public async Task Index_FiltersSearchTextAndTags(string query, params string[] expectedTitles)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var author = new User
        {
            Id = "author",
            UserName = "author",
            Bio = "",
            ProfilePicture = new Uri("https://example.com/profile.png")
        };
        context.SUThreads.AddRange(
            new SUThread
            {
                Title = "Collections",
                Content = "Fix enumeration errors",
                User = author,
                ThreadTags = [new ThreadTag { Tag = new Tag { Name = "csharp" } }]
            },
            new SUThread
            {
                Title = "Database",
                Content = "Configure a connection",
                User = author,
                ThreadTags = [new ThreadTag { Tag = new Tag { Name = "dotnet" } }]
            },
            new SUThread
            {
                Title = "Collections without tags",
                Content = "General discussion",
                User = author
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var controller = new HomeController(context);
        var result = Assert.IsType<ViewResult>(await controller.Index(query));
        var model = Assert.IsType<HomeViewModel>(result.Model);

        Assert.Equal(expectedTitles.OrderBy(title => title), model.Threads.Select(thread => thread.Title).OrderBy(title => title));
        var words = query.Split(' ');
        var expectedText = string.Join(" ", words.Where(word => !word.StartsWith('#')));
        var expectedTags = words.Where(word => word.StartsWith('#')).Select(word => word[1..]).Distinct().ToArray();
        Assert.Equal(expectedText, controller.ViewData["SearchText"]);
        Assert.Equal(expectedTags, Assert.IsType<List<string>>(controller.ViewData["SearchTags"]));

        var retainedResult = Assert.IsType<ViewResult>(await controller.Index(expectedText, expectedTags));
        var retainedModel = Assert.IsType<HomeViewModel>(retainedResult.Model);
        Assert.Equal(expectedTitles.OrderBy(title => title), retainedModel.Threads.Select(thread => thread.Title).OrderBy(title => title));

        var api = new StackUnderflow.Areas.Api.ThreadController(context, new StackUnderflow.Services.ThreadVoteService(context));
        var response = await api.GetSUThreadsPaginated(new PaginationRequest(), (string?)controller.ViewData["Query"]);
        var page = Assert.IsType<PaginatedResponse<ThreadSummaryDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(expectedTitles.OrderBy(title => title), page.Data.Select(thread => thread.Title).OrderBy(title => title));

        var removedResult = Assert.IsType<ViewResult>(await controller.Index("Collections", ["csharp"], "csharp"));
        var removedModel = Assert.IsType<HomeViewModel>(removedResult.Model);
        Assert.Equal(2, removedModel.Threads.Count);
        Assert.Empty(Assert.IsType<List<string>>(controller.ViewData["SearchTags"]));
        Assert.Equal("Collections", controller.ViewData["Query"]);
    }

    [Fact]
    public async Task Index_RemovingLastTagWithoutTextReturnsToUnfilteredHome()
    {
        var controller = new HomeController(null!);
        var result = Assert.IsType<RedirectToActionResult>(await controller.Index("", ["csharp"], "csharp"));
        Assert.Equal("Index", result.ActionName);
    }
}
