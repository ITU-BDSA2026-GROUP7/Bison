using System.ComponentModel.DataAnnotations;
using Bison.Core.Domain;
using Bison.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class UserTimelineModel : PageModel
{
    private readonly IObservationService _service;
    private readonly IVoteService _voteService;

    private readonly ITaxonService _taxonService;

    private readonly UserManager<Author> _userManager;
    private readonly SignInManager<Author> _signInManager;
    private readonly ObservationPictureStorage _pictureStorage;

    public List<ObservationViewModel> Observations { get; set; } = new();
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }

    // True when a logged-in user is looking at their own timeline
    // ("My Observations"). Only then is the input field shown.
    public bool IsOwnTimeline { get; set; }

    // The taxa offered in the "What did you see?" dropdown (only loaded on
    // your own timeline). Genera without a Danish name are left out.
    public List<TaxonViewModel> NamedTaxa { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Write what you saw before sharing.")]
    public string Text { get; set; } = string.Empty;

    // The taxon picked in the form. Empty means "Don't know".
    [BindProperty]
    public string? ObservedTaxonId { get; set; }

    [BindProperty]
    public IFormFile? Picture { get; set; }

    public UserTimelineModel(
        IObservationService service,
        IVoteService voteService,
        ITaxonService taxonService,
        UserManager<Author> userManager,
        SignInManager<Author> signInManager,
        ObservationPictureStorage pictureStorage)
    {
        _service = service;
        _voteService = voteService;
        _taxonService = taxonService;
        _userManager = userManager;
        _signInManager = signInManager;
        _pictureStorage = pictureStorage;
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

        // Same check as on the public page: only accept taxa that exist.
        if (!string.IsNullOrEmpty(ObservedTaxonId) &&
            !_taxonService.GetTaxa().Any(t => t.DwcTaxonId == ObservedTaxonId))
        {
            ModelState.AddModelError(nameof(ObservedTaxonId), "Pick a taxon from the list.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTimelineAsync(author, 1);
            return Page();
        }

        var me = await _userManager.GetUserAsync(User);
        if (me == null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        // The observation is always saved as the logged-in user,
        // no matter whose timeline the form was posted from.
        await using var pictureStream = Picture?.OpenReadStream();
        var upload = await _pictureStorage.SaveAsync(pictureStream, Picture?.Length ?? 0);
        if (upload.ErrorMessage is not null)
        {
            ModelState.AddModelError(nameof(Picture), upload.ErrorMessage);
            await LoadTimelineAsync(author, 1);
            return Page();
        }

        _service.CreateObservation(me.Id, Text, ObservedTaxonId, upload.PicturePath);

        return RedirectToPage(new { author });
    }

    public async Task<IActionResult> OnPostVoteAsync(string author, int postId, bool direction, int page = 1)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        if (direction)
        {
            _voteService.Upvote(postId, user.Id);
        }
        else
        {
            _voteService.Downvote(postId, user.Id);
        }

        return RedirectToPage("/UserTimeline", new { author, page = Math.Max(page, 1) });
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

        // Only your own timeline shows the form, so only load taxa then.
        if (IsOwnTimeline)
        {
            NamedTaxa = _taxonService.GetTaxa()
                .Where(t => !string.IsNullOrEmpty(t.DanishVernacularName))
                .ToList();
        }

        var result = _service.GetObservationsFromAuthor(author, page);

        Observations = result.Observations;
        HasNextPage = result.HasNextPage;
    }
}
