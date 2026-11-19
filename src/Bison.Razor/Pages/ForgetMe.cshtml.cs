using Bison.Core.Domain;
using Bison.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

// [Authorize]: only a logged-in user can forget themselves, and only themselves.
[Authorize]
public class ForgetMeModel : PageModel
{
    private readonly IUserInfoService _service;
    private readonly UserManager<Author> _userManager;
    private readonly SignInManager<Author> _signInManager;

    // "yes" = keep my observations online, "no" = delete them too.
    // No default on purpose: the user has to choose.
    [BindProperty]
    public string? KeepObservations { get; set; }

    public string? ErrorMessage { get; private set; }

    public ForgetMeModel(
        IUserInfoService service,
        UserManager<Author> userManager,
        SignInManager<Author> signInManager)
    {
        _service = service;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // The id comes from the login cookie, never from the form.
        var me = await _userManager.GetUserAsync(User);
        if (me == null)
        {
            return Challenge();
        }

        if (KeepObservations is not ("yes" or "no"))
        {
            ErrorMessage = "Choose what should happen to your observations.";
            return Page();
        }

        _service.ForgetUser(me.Id, keepObservations: KeepObservations == "yes");

        // The account no longer exists, so end the session.
        await _signInManager.SignOutAsync();
        return Redirect("/obs");
    }
}