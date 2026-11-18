using Bison.Core.Domain;
using Bison.Core.DTO;
using Bison.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

// [Authorize] makes ASP.NET send anyone who is not logged in to the
// login page. Hiding the link alone would not protect the page:
// anyone could still type /aboutme into the address bar.
[Authorize]
public class AboutMeModel : PageModel
{
    private readonly IUserInfoService _service;
    private readonly UserManager<Author> _userManager;

    public UserInfoDTO Info { get; set; } = new();

    public AboutMeModel(IUserInfoService service, UserManager<Author> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Always show the data of the logged-in user, never someone else's.
        var me = await _userManager.GetUserAsync(User);
        if (me == null)
        {
            return Challenge();
        }

        var info = _service.GetUserInfo(me.Id);
        if (info == null)
        {
            return Challenge();
        }

        Info = info;
        return Page();
    }
}
