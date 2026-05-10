using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace StackUnderflow.Models;

public class Post
{
    [Key]
    public int Id { get; set; }
    
    public string Content { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    
    public bool IsAcceptedAnswer { get; set; }
    
    public string UserId { get; set; }
    public User User { get; set; }
    
    public int SUThreadId { get; set; }
    public SUThread SUThread { get; set; }
    
    public ICollection<Comment> Comments { get; set; }
}