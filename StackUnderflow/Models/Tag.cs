using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Models;

public class Tag
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ThreadTag> ThreadTags { get; set; } = [];
}
