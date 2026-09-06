using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackUnderflow.Areas.Api.Controllers;
using StackUnderflow.Data;
using StackUnderflow.Models;
using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Tests;

/// <summary>
/// Builds a <see cref="ProfileImageController"/> over an in-memory SQLite database.
/// Nothing here touches Azure, so every test runs with no credentials present.
/// </summary>
public sealed class ProfileImageTestContext : IDisposable
{
    // Shared across the fixture so ControllerBase.Problem() can resolve a
    // ProblemDetailsFactory the same way it would inside the real pipeline.
    private static readonly IServiceProvider MvcServices = BuildMvcServices();

    private readonly SqliteConnection _connection;

    public ProfileImageTestContext()
    {
        // A connection held open keeps the in-memory database alive for the test.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public ApplicationDbContext DbContext { get; }

    /// <summary>Adds a user with the external placeholder avatar the seeder uses.</summary>
    public async Task<User> AddUserAsync(string id, string? profileImagePath = null)
    {
        var user = new User
        {
            Id = id,
            UserName = $"{id}@example.com",
            Email = $"{id}@example.com",
            Bio = string.Empty,
            JoinDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProfilePicture = new Uri("https://i.pravatar.cc/160?img=1"),
            ProfileImagePath = profileImagePath,
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    /// <summary>Creates the controller acting as <paramref name="signedInUserId"/>, or anonymously when null.</summary>
    public ProfileImageController CreateController(IProfileImageStorage storage, string? signedInUserId)
    {
        var identity = signedInUserId is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, signedInUserId)], "TestAuth");

        var controller = new ProfileImageController(
            DbContext,
            storage,
            NullLogger<ProfileImageController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
                RequestServices = MvcServices,
            },
        };

        return controller;
    }

    private static IServiceProvider BuildMvcServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }
}