using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services;

namespace StackUnderflow.Tests;

public sealed class ThreadPaginationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task GetSUThreadsPaginated_IncludesTagNamesOnLaterPages(int tagCount)
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
        var names = new[] { "csharp", "<b>dotnet</b>" }.Take(tagCount).ToArray();
        var older = new SUThread
        {
            Title = "Older thread",
            User = author,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ThreadTags = names.Select(name => new ThreadTag { Tag = new Tag { Name = name } }).ToList()
        };
        context.SUThreads.AddRange(older, new SUThread
        {
            Title = "Newer thread",
            User = author,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var controller = new StackUnderflow.Areas.Api.ThreadController(context, new ThreadVoteService(context));
        var response = await controller.GetSUThreadsPaginated(new PaginationRequest { Page = 2, PageSize = 1 });
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var page = Assert.IsType<PaginatedResponse<ThreadSummaryDto>>(result.Value);
        var thread = Assert.Single(page.Data);
        Assert.Equal(older.Id, thread.Id);
        Assert.Equal(names.OrderBy(name => name), thread.Tags.OrderBy(name => name));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(page, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(names.OrderBy(name => name), json.RootElement.GetProperty("data")[0]
            .GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).OrderBy(name => name));
    }
}
