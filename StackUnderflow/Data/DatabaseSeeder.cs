using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Models;

namespace StackUnderflow.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var seedEnabled = configuration.GetValue<bool?>("Database:Seed") ?? environment.IsDevelopment();
        if (!seedEnabled)
        {
            return;
        }

        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var userManager = services.GetRequiredService<UserManager<User>>();
        var now = DateTime.UtcNow;

        // NOTE: Only intended for local/dev use. Gate via Database:Seed or ASPNETCORE_ENVIRONMENT=Development.
        const string defaultPassword = "Passw0rd!";

        var alice = await EnsureUserAsync(
            userManager,
            email: "alice@example.com",
            userName: "alice@example.com",
            password: defaultPassword,
            bio: "I write questions that accidentally become documentation.",
            profilePicture: new Uri("https://example.com/alice.png"),
            joinDate: now.AddDays(-60),
            reputation: 1200);

        var bob = await EnsureUserAsync(
            userManager,
            email: "bob@example.com",
            userName: "bob@example.com",
            password: defaultPassword,
            bio: "I answer fast, then edit later.",
            profilePicture: new Uri("https://example.com/bob.png"),
            joinDate: now.AddDays(-20),
            reputation: 420);

        if (await dbContext.SUThreads.AnyAsync(cancellationToken))
        {
            return;
        }

        var thread = new SUThread
        {
            Title = "How do I seed an EF Core database on startup?",
            Content = "I have an ASP.NET Core app with Identity + EF Core. What's the clean way to seed sample data for local development?",
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3),
            ViewCount = 42,
            UpvoteCount = 7,
            DownvoteCount = 0,
            IsSolved = true,
            UserId = alice.Id,
        };

        var answer = new Post
        {
            Content = "Create a scoped seeder that runs in Development, ensures the database exists (or migrates), then inserts data only if missing.",
            CreatedAt = now.AddDays(-3).AddHours(1),
            UpdatedAt = now.AddDays(-3).AddHours(1),
            Upvotes = 5,
            Downvotes = 0,
            IsAcceptedAnswer = true,
            UserId = bob.Id,
            SUThread = thread,
            Comments = new List<Comment>(),
        };

        answer.Comments.Add(new Comment
        {
            Content = "This worked perfectly for my local SQLite setup.",
            CreatedAt = now.AddDays(-3).AddHours(2),
            UpdatedAt = now.AddDays(-3).AddHours(2),
            UserId = alice.Id,
            Post = answer,
        });

        dbContext.SUThreads.Add(thread);
        dbContext.Posts.Add(answer);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> userManager,
        string email,
        string userName,
        string password,
        string bio,
        Uri profilePicture,
        DateTime joinDate,
        int reputation)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                Bio = bio,
                ProfilePicture = profilePicture,
                JoinDate = joinDate,
                Reputation = reputation,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            return user;
        }

        var updated = false;

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            updated = true;
        }

        if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            user.UserName = userName;
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(user.Bio))
        {
            user.Bio = bio;
            updated = true;
        }

        if (user.ProfilePicture is null)
        {
            user.ProfilePicture = profilePicture;
            updated = true;
        }

        if (user.JoinDate == default)
        {
            user.JoinDate = joinDate;
            updated = true;
        }

        if (user.Reputation == default)
        {
            user.Reputation = reputation;
            updated = true;
        }

        if (updated)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        return user;
    }
}
