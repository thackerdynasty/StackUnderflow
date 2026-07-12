using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Models;

namespace StackUnderflow.Data;

public static class DatabaseSeeder
{
    private const string DefaultPassword = "Passw0rd!";

    private static readonly UserSeed[] Users =
    [
        new("alice@example.com", "alice@example.com", "I write questions that accidentally become documentation.", "https://i.pravatar.cc/160?img=1", -180, 1850),
        new("bob@example.com", "bob@example.com", "I answer fast, then edit later.", "https://i.pravatar.cc/160?img=2", -160, 1420),
        new("carla@example.com", "carla@example.com", "Backend dev, SQL whisperer, migration survivor.", "https://i.pravatar.cc/160?img=3", -140, 980),
        new("devon@example.com", "devon@example.com", "Frontend engineer who cares too much about tiny UI states.", "https://i.pravatar.cc/160?img=4", -120, 760),
        new("emery@example.com", "emery@example.com", "I break auth locally so production users do not have to.", "https://i.pravatar.cc/160?img=5", -96, 640),
        new("fatima@example.com", "fatima@example.com", "Testing, diagnostics, and asking what the logs actually say.", "https://i.pravatar.cc/160?img=6", -72, 520),
        new("gabe@example.com", "gabe@example.com", "I like simple controllers and boring deployment scripts.", "https://i.pravatar.cc/160?img=7", -54, 410),
        new("hana@example.com", "hana@example.com", "CSS, Razor, accessibility, and making hover states behave.", "https://i.pravatar.cc/160?img=8", -41, 360),
        new("ivan@example.com", "ivan@example.com", "New to ASP.NET, not new to reading stack traces.", "https://i.pravatar.cc/160?img=9", -28, 220),
        new("jules@example.com", "jules@example.com", "I turn vague bugs into tiny reproducible examples.", "https://i.pravatar.cc/160?img=10", -18, 190),
    ];

    private static readonly ThreadSeed[] Threads =
    [
        new("How do I seed an EF Core database on startup?", "I want local data every time the app starts in Development. Is this pattern okay?\n\n```csharp\nusing var scope = app.Services.CreateScope();\nawait DatabaseSeeder.SeedAsync(scope.ServiceProvider);\n```"),
        new("Why does my Razor view show encoded HTML instead of code blocks?", "My parser returns HTML for fenced snippets, but the page prints the tags. I am trying to understand whether this belongs in the parser or in the Razor view."),
        new("How should I sort accepted answers before high score answers?", "I need accepted answers first, then score, then oldest.\n\n```csharp\nposts.OrderByDescending(p => p.IsAcceptedAnswer)\n     .ThenByDescending(p => p.Upvotes - p.Downvotes)\n     .ThenBy(p => p.CreatedAt);\n```"),
        new("Bootstrap card grid looks cramped inside a hover popup", "The full profile card works, but the popup needs smaller stats, tighter labels, and less padding. The page version should stay unchanged."),
        new("How do I prevent users from voting on their own posts?", "My controller disables the button in Razor, but I want the server to enforce it too.\n\n```csharp\nif (post.UserId == userId) return Forbid();\n```"),
        new("Entity Framework navigation collection is null after seeding", "I add comments to a post during seeding, then a view reads a null collection. Is it better to initialize collections in the model or every time I create seed objects?"),
        new("What is the cleanest route for a profile hover card partial?", "I want a username hover to fetch HTML from MVC and reuse the existing partial.\n\n```js\nfetch(`/Profile/InfoCard/${userId}`)\n```"),
        new("Why is my localhost page connection refused in VS Code?", "The browser opens port 8080, but ASP.NET says it is listening on 5142. I switched branches and now the launch profile seems out of sync."),
        new("How do I make comments render under each answer?", "I include comments on posts but the UI sometimes shows them out of order.\n\n```csharp\npost.Comments.OrderBy(c => c.CreatedAt)\n```"),
        new("Can I use triple backticks in a textarea preview?", "I want users to type markdown-like code fences and see a preview.\n\n```js\nconst fenceRegex = /```([\\s\\S]*?)```/g;\n```"),
        new("How should reputation update when an answer is accepted?", "Accepted answer reputation and thread owner reputation should both change, but I am unsure whether to calculate it live or store the value."),
        new("Why does my Identity user have null custom fields?", "I added Bio and ProfilePicture to User but old users have empty values.\n\n```csharp\npublic string Bio { get; set; }\npublic Uri ProfilePicture { get; set; }\n```"),
        new("How do I keep a hover card visible while moving the mouse?", "The card disappears between the link and popup unless I delay hiding it. I want it to feel forgiving without sticking around forever."),
        new("Should API update endpoints accept null values?", "My DTO treats null as leave unchanged. Is that okay for profile fields?\n\n```csharp\nif (dto.Bio is not null) user.Bio = dto.Bio;\n```"),
        new("How do I show initials when an avatar image fails?", "I want a fallback if the profile image URL cannot load, preferably without needing a second request or a placeholder image."),
        new("Why does dotnet build fail while debugging?", "The debugger locks the DLL and the compiler cannot copy the new one.\n\n```powershell\ndotnet build -p:OutDir=C:\\tmp\\build\\\n```"),
        new("What should be included in a StackOverflow-like question summary?", "I need votes, answers, views, solved status, title, excerpt, and author. I am trying to keep it dense but still readable."),
        new("How do I add a live preview for answer edits?", "I have preview working for new answers, but edit forms are generated per post.\n\n```js\nattachLivePreview('#post-edit-42', '#post-edit-preview-42');\n```"),
        new("How can I keep seeded view counts realistic?", "The homepage looks fake when every question has 42 views. I want view counts to roughly follow activity without needing random data."),
        new("How should I validate email on an API DTO?", "I want basic validation without exposing Identity internals.\n\n```csharp\n[EmailAddress]\npublic string? Email { get; set; }\n```"),
        new("How do I make answer sorting preserve accepted answers?", "When users choose newest, accepted answers should still stay first. The accepted answer should not jump into the middle of the list."),
        new("What is the safest way to delete a user in a demo API?", "The route works, but relationships and permissions make me nervous.\n\n```csharp\n_dbContext.Users.Remove(user);\nawait _dbContext.SaveChangesAsync();\n```"),
        new("How can I render code fences without allowing script tags?", "I want code blocks, not arbitrary HTML injection. What is the safest way to turn user text into a rendered post?"),
        new("How do I make profile-card data feel populated?", "A card with reputation but no answers or comments feels empty. I want seeded data that exercises profile summaries, hover cards, accepted answers, and comments."),
    ];

    // Topical tags for each thread, parallel to the Threads array above.
    private static readonly string[][] ThreadTagNames =
    [
        ["ef-core", "seeding"],
        ["razor", "markdown"],
        ["ef-core", "c#"],
        ["bootstrap", "css"],
        ["security", "voting"],
        ["ef-core", "seeding"],
        ["asp.net-core", "javascript"],
        ["debugging", "asp.net-core"],
        ["razor", "ef-core"],
        ["javascript", "markdown"],
        ["reputation", "c#"],
        ["identity", "ef-core"],
        ["javascript", "css"],
        ["api", "validation"],
        ["css", "javascript"],
        ["debugging", "dotnet"],
        ["razor", "ui"],
        ["javascript", "razor"],
        ["seeding", "c#"],
        ["api", "validation"],
        ["c#", "ef-core"],
        ["api", "security"],
        ["security", "markdown"],
        ["seeding", "ui"],
    ];

    private static readonly string[] AnswerTemplates =
    [
        "Start by making the behavior explicit and keeping the controller small.\n\n```csharp\nif (model is null) return NotFound();\nreturn View(model);\n```",
        "I would put this behind a helper so the view stays readable. The controller should describe the workflow, not every tiny formatting decision.",
        "The important part is to query only the data the view needs.\n\n```csharp\n.AsNoTracking()\n.Include(x => x.User)\n.ToListAsync();\n```",
        "This is a good place for a tiny client-side enhancement. Keep the HTML usable first, then add hover behavior on top of normal profile links.",
        "Make sure the seeded objects have both the foreign key and navigation where useful.\n\n```csharp\npost.SUThread = thread;\npost.UserId = answerAuthor.Id;\n```",
        "For UI polish, clamp the popup position to the viewport. That prevents it from disappearing off the right side on narrower screens.",
        "I usually verify this with a small known input first.\n\n```csharp\nvar input = \"```\\nprint(\\\"hello world\\\")\\n```\";\n```",
        "If this is for Development seeding, make it deterministic instead of random. Repeatable data makes screenshots and debugging much easier.",
        "Do not rely only on disabled buttons. Keep the server check.\n\n```csharp\nif (entity.UserId == currentUserId) return Forbid();\n```",
        "The route can return partial HTML as long as the caller treats it as trusted server-rendered markup.",
        "For this case, I would keep the API DTO and the page view model separate. They serve different shapes of data.",
        "A little sample data variety goes a long way here: unsolved questions, quiet posts, busy comment threads, and a few accepted answers.",
        "You can make the profile card feel more real by spreading answers and comments across different seeded users.",
        "If the rendered page feels noisy, reduce the card chrome before removing useful data.",
    ];

    private static readonly string[] CommentTemplates =
    [
        "This helped me reproduce it locally.",
        "Small note: this also works with the seeded demo users.",
        "The code fence example made the issue much clearer.",
        "I would add a server-side check for this too.",
        "This answer fixed the UI state for me.",
        "Watch out for null navigation collections here.",
        "The view count formula makes the homepage feel less flat.",
        "This is also useful for testing the profile hover card.",
        "I hit the same thing after switching branches.",
        "This should probably be covered by the seed data.",
        "The accepted-answer ordering is the part I missed.",
        "Nice, this keeps the profile page and hover card in sync.",
        "I would leave the plain-text examples too so the parser is not the only thing being tested.",
        "This makes the home page feel much less artificial.",
    ];

    private static readonly int[] CommentPattern = [0, 3, 1, 5, 0, 2, 4, 1, 0, 2, 5, 0, 3, 1, 4, 0];

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

        var users = new List<User>();
        foreach (var seed in Users)
        {
            users.Add(await EnsureUserAsync(
                userManager,
                email: seed.Email,
                userName: seed.UserName,
                password: DefaultPassword,
                bio: seed.Bio,
                profilePicture: new Uri(seed.ProfilePicture),
                joinDate: now.AddDays(seed.JoinedDaysAgo),
                reputation: seed.Reputation));
        }

        if (await dbContext.SUThreads.AnyAsync(cancellationToken))
        {
            return;
        }

        var tagLookup = new Dictionary<string, Tag>();

        for (var threadIndex = 0; threadIndex < Threads.Length; threadIndex++)
        {
            var seed = Threads[threadIndex];
            var author = users[threadIndex % users.Count];
            var postCount = threadIndex % 10 + 1;
            var createdAt = now.AddDays(-45 + threadIndex).AddHours(-(threadIndex % 8));
            var downvotes = threadIndex % 5 == 0 ? 1 : 0;
            var upvotes = 2 + threadIndex % 8;
            var commentCount = 0;

            var thread = new SUThread
            {
                Title = seed.Title,
                Content = seed.Content,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddHours(postCount * 3),
                UpvoteCount = upvotes,
                DownvoteCount = downvotes,
                IsSolved = threadIndex % 4 != 1,
                UserId = author.Id,
                User = author,
                Posts = new List<Post>(),
                Votes = new List<ThreadVote>(),
            };

            for (var voteIndex = 0; voteIndex < upvotes + downvotes; voteIndex++)
            {
                var voter = users[(threadIndex + voteIndex + 1) % users.Count];
                thread.Votes.Add(new ThreadVote
                {
                    Value = voteIndex < upvotes ? 1 : -1,
                    CreatedAt = createdAt.AddMinutes(20 + voteIndex),
                    UpdatedAt = createdAt.AddMinutes(20 + voteIndex),
                    UserId = voter.Id,
                    User = voter,
                    SUThread = thread,
                });
            }

            for (var postIndex = 0; postIndex < postCount; postIndex++)
            {
                var answerAuthor = users[(threadIndex + postIndex + 2) % users.Count];
                var postCreatedAt = createdAt.AddHours(postIndex + 1);
                var postUpvotes = Math.Max(0, upvotes - postIndex / 2);
                var postDownvotes = postIndex % 6 == 0 ? 1 : 0;
                var isAccepted = thread.IsSolved && postIndex == 0;

                var post = new Post
                {
                    Content = AnswerTemplates[(threadIndex + postIndex) % AnswerTemplates.Length],
                    CreatedAt = postCreatedAt,
                    UpdatedAt = postCreatedAt.AddMinutes(18),
                    Upvotes = postUpvotes,
                    Downvotes = postDownvotes,
                    IsAcceptedAnswer = isAccepted,
                    UserId = answerAuthor.Id,
                    User = answerAuthor,
                    SUThread = thread,
                    Comments = new List<Comment>(),
                    Votes = new List<PostVote>(),
                };

                var postVoteTotal = Math.Min(users.Count - 1, postUpvotes + postDownvotes);
                for (var voteIndex = 0; voteIndex < postVoteTotal; voteIndex++)
                {
                    var voter = users[(threadIndex + postIndex + voteIndex + 3) % users.Count];
                    post.Votes.Add(new PostVote
                    {
                        Value = voteIndex < postUpvotes ? 1 : -1,
                        CreatedAt = postCreatedAt.AddMinutes(8 + voteIndex),
                        UpdatedAt = postCreatedAt.AddMinutes(8 + voteIndex),
                        UserId = voter.Id,
                        User = voter,
                        Post = post,
                    });
                }

                var commentsForPost = threadIndex == Threads.Length - 1
                    ? 0
                    : CommentPattern[(threadIndex * 3 + postIndex) % CommentPattern.Length];

                for (var commentIndex = 0; commentIndex < commentsForPost; commentIndex++)
                {
                    var commenter = users[(threadIndex + postIndex + commentIndex + 4) % users.Count];
                    var commentCreatedAt = postCreatedAt.AddMinutes(45 + commentIndex * 20);
                    post.Comments.Add(new Comment
                    {
                        Content = CommentTemplates[(threadIndex + postIndex + commentIndex) % CommentTemplates.Length],
                        CreatedAt = commentCreatedAt,
                        UpdatedAt = commentCreatedAt,
                        UserId = commenter.Id,
                        User = commenter,
                        Post = post,
                    });
                    commentCount++;
                }

                thread.Posts.Add(post);
            }

            foreach (var tagName in ThreadTagNames[threadIndex])
            {
                if (!tagLookup.TryGetValue(tagName, out var tag))
                {
                    tag = new Tag { Name = tagName };
                    tagLookup[tagName] = tag;
                }

                thread.ThreadTags.Add(new ThreadTag { Tag = tag, SUThread = thread });
            }

            thread.ViewCount = 35 + upvotes * 40 + postCount * 24 + commentCount * 18 + threadIndex * 11;
            dbContext.SUThreads.Add(thread);
        }

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

    private sealed record UserSeed(
        string Email,
        string UserName,
        string Bio,
        string ProfilePicture,
        int JoinedDaysAgo,
        int Reputation);

    private sealed record ThreadSeed(string Title, string Content);
}
