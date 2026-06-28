using System.ComponentModel.DataAnnotations;

namespace StackUnderflow.Areas.Api.Models;

// Read model returned to clients. Deliberately excludes the sensitive fields on
// IdentityUser (password hash, security stamp, lockout state, etc.).
public class UserDto
{
    public string Id { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public int Reputation { get; set; }
    public DateTime JoinDate { get; set; }
    public Uri? ProfilePicture { get; set; }
    public string? Bio { get; set; }
}

// Payload for updating an existing user. All fields optional so callers can
// patch a subset; null values leave the corresponding column untouched.
public class UpdateUserDto
{
    [MaxLength(256)]
    public string? UserName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public Uri? ProfilePicture { get; set; }

    public string? Bio { get; set; }
}
