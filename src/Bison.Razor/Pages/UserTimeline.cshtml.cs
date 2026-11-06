using System.ComponentModel.DataAnnotations;
using Bison.Core.Domain;
using Bison.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class UserTimelineModel : PageModel
{
    private readonly IObservationService _service;

    private readonly UserManager<Author> _userManager;

    public List<ObservationViewModel> Observations { get; set; } = new();
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }

    // True when a logged-in user is looking at their own timeline
    // ("My Observations"). Only then is the input field shown.
    public bool IsOwnTimeline { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Write what you saw before sharing.")]
    public string Text { get; set; } = string.Empty;

    public UserTimelineModel(IObservationService service, UserManager<Author> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    public async Task<ActionResult> OnGetAsync(string author, [FromQuery] int page = 1)
    {
        await LoadTimelineAsync(author, page);
        return Page();
    }

    public async Task<ActionResult> OnPostAsync(string author)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadTimelineAsync(author, 1);
            return Page();
        }

        var me = await _userManager.GetUserAsync(User);
        if (me == null)
        {
            return Challenge();
        }

        // The observation is always saved as the logged-in user,
        // no matter whose timeline the form was posted from.
        _service.CreateObservation(me.Id, Text);

        return RedirectToPage(new { author });
    }

    private async Task LoadTimelineAsync(string author, int page)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;

        if (User.Identity?.IsAuthenticated == true)
        {
            var me = await _userManager.GetUserAsync(User);
            IsOwnTimeline = me != null && me.Name == author;
        }

        var result = _service.GetObservationsFromAuthor(author, page);

        Observations = result.Observations;
        HasNextPage = result.HasNextPage;
    }
}
