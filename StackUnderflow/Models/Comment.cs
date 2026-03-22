using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace StackUnderflow.Models;

public class Comment
{
    [Key]
    public string Id { get; set; }
    
    public string Content { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime DeletedOn { get; set; }
    
    public string UserId { get; set; }
    public IdentityUser User { get; set; }
    
    public int PostId { get; set; }
    public Post Post { get; set; }
}