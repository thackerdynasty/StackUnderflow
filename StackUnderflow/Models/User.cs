using Microsoft.AspNetCore.Identity;

namespace StackUnderflow.Models;

public class User : IdentityUser
{
    public int Reputation { get; set; }
    public DateTime JoinDate { get; set; }
    public Uri ProfilePicture { get; set; }

    /// <summary>
    /// Relative path of an uploaded avatar within the blob container, for example
    /// "a3f2.../9f3c-....jpg". Null when the user has never uploaded one, in which
    /// case <see cref="ProfilePicture"/> is used instead. Stored relative so the
    /// storage account can change, or a CDN can be added, without a data migration.
    /// </summary>
    public string? ProfileImagePath { get; set; }

    public string Bio { get; set; }
    
    public ICollection<Post> Posts { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<SUThread> SUThreads { get; set; }
    public ICollection<ThreadVote> ThreadVotes { get; set; }
    public ICollection<PostVote> PostVotes { get; set; }
    public ICollection<SavedThread> SavedThreads { get; set; }
}
