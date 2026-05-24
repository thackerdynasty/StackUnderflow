using Microsoft.AspNetCore.Identity;

namespace StackUnderflow.Models;

public class User : IdentityUser
{
    public int Reputation { get; set; }
    public DateTime JoinDate { get; set; }
    public Uri ProfilePicture { get; set; }
    public string Bio { get; set; }
    
    public ICollection<Post> Posts { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<SUThread> SUThreads { get; set; }
    public ICollection<ThreadVote> ThreadVotes { get; set; }
    public ICollection<PostVote> PostVotes { get; set; }
}
