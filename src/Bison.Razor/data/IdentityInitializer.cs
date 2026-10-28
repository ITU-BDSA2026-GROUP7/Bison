using Bison.Razor.Models;
using Microsoft.AspNetCore.Identity;

namespace Bison.Razor.Data;

// Creates the two login users required by the week 10 project work.
public static class IdentityInitializer
{
    public static async Task SeedUsersAsync(UserManager<Author> userManager)
    {
        await CreateUserIfMissingAsync(userManager, "Eduard", "edka@itu.dk", "123456");
        await CreateUserIfMissingAsync(userManager, "Debasmita", "debloh@itu.dk", "password");
    }

    private static async Task CreateUserIfMissingAsync(
        UserManager<Author> userManager, string name, string email, string password)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var author = new Author
        {
            Name = name,
            // The default Login page signs in by UserName using what the user
            // typed in the "Email" box, so UserName must be the email address.
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        // CreateAsync hashes the password; it is never stored in plain text.
        var result = await userManager.CreateAsync(author, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create user {email}: {errors}");
        }
    }
}
