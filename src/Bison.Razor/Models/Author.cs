using Microsoft.AspNetCore.Identity;

namespace Bison.Razor.Models;

// An Author is also a user who can log in. IdentityUser<int> gives us
// Id (int), UserName, Email, PasswordHash, etc., so we only add what
// Bison itself needs on top.
public class Author : IdentityUser<int>
{
    // Display name shown on observations and used in /obs/{author} URLs.
    public string Name { get; set; } = string.Empty;

    public List<Post> Posts { get; set; } = new();
}
