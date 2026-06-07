using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Models;

namespace StackUnderflow.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<SUThread> SUThreads { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ThreadVote> ThreadVotes { get; set; }
    public DbSet<PostVote> PostVotes { get; set; }
    public DbSet<SavedThread> SavedThreads { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Post>()
            .HasOne(p => p.SUThread)
            .WithMany(t => t.Posts)
            .HasForeignKey(p => p.SUThreadId)
            .OnDelete(DeleteBehavior.NoAction);
    
        builder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    
        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<SUThread>()
            .HasOne(t => t.User)
            .WithMany(u => u.SUThreads)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ThreadVote>()
            .HasOne(v => v.User)
            .WithMany(u => u.ThreadVotes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ThreadVote>()
            .HasOne(v => v.SUThread)
            .WithMany(t => t.Votes)
            .HasForeignKey(v => v.SUThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ThreadVote>()
            .HasIndex(v => new { v.UserId, v.SUThreadId })
            .IsUnique();

        builder.Entity<PostVote>()
            .HasOne(v => v.User)
            .WithMany(u => u.PostVotes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostVote>()
            .HasOne(v => v.Post)
            .WithMany(p => p.Votes)
            .HasForeignKey(v => v.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostVote>()
            .HasIndex(v => new { v.UserId, v.PostId })
            .IsUnique();

        builder.Entity<SavedThread>()
            .HasOne(s => s.User)
            .WithMany(u => u.SavedThreads)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<SavedThread>()
            .HasOne(s => s.SUThread)
            .WithMany(t => t.SavedBy)
            .HasForeignKey(s => s.SUThreadId)
            .OnDelete(DeleteBehavior.NoAction);

        // A user can save a given thread only once.
        builder.Entity<SavedThread>()
            .HasIndex(s => new { s.UserId, s.SUThreadId })
            .IsUnique();
    }
}
