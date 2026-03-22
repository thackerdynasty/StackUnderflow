using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace StackUnderflow.Models;

public class Post
{
    [Key]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime Date { get; set; }
    
    public string UserId { get; set; }
    public IdentityUser User { get; set; }
    
    public ICollection<Comment> Comments { get; set; }
}