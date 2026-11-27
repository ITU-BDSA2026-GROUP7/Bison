using System.ComponentModel.DataAnnotations;
using Bison.Core.Domain;
using Bison.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly IObservationService _service;
    private readonly IVoteService _voteService;

    private readonly ITaxonService _taxonService;

    private readonly UserManager<Author> _userManager;
    private readonly SignInManager<Author> _signInManager;
    private readonly ObservationPictureStorage _pictureStorage;

    public List<TaxonViewModel> Taxa { get; set; } = new();

    // The taxa offered in the "What did you see?" dropdown. Genera like
    // "Ardea" have no Danish name, so they are left out to avoid blank options.
    public List<TaxonViewModel> NamedTaxa =>
        Taxa.Where(t => !string.IsNullOrEmpty(t.DanishVernacularName)).ToList();

    public string? SelectedTaxonId { get; set; }

    public List<ObservationViewModel> Observations { get; set; } = new();
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }

    // Filled from the "What did you see?" form when it is posted.
    [BindProperty]
    [Required(ErrorMessage = "Write what you saw before sharing.")]
    public string Text { get; set; } = string.Empty;

    // The taxon picked in the form. Empty means "Don't know".
    [BindProperty]
    public string? ObservedTaxonId { get; set; }

    [BindProperty]
    public IFormFile? Picture { get; set; }

    public PublicModel(
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

    public ActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] string? taxonId = null)
    {
        LoadTimeline(page, taxonId);
        return Page();
    }

    public async Task<ActionResult> OnPostAsync()
    {
        // Only logged-in users may make observations.
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        // The dropdown only offers real taxa, but a hand-made request could
        // send any id, so check it and show a friendly error instead of crashing.
        if (!string.IsNullOrEmpty(ObservedTaxonId) &&
            !_taxonService.GetTaxa().Any(t => t.DwcTaxonId == ObservedTaxonId))
        {
            ModelState.AddModelError(nameof(ObservedTaxonId), "Pick a taxon from the list.");
        }

        if (!ModelState.IsValid)
        {
            // Show the page again, with the error message next to the form.
            LoadTimeline(1, null);
            return Page();
        }

        var author = await _userManager.GetUserAsync(User);
        if (author is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        await using var pictureStream = Picture?.OpenReadStream();
        var upload = await _pictureStorage.SaveAsync(pictureStream, Picture?.Length ?? 0);
        if (upload.ErrorMessage is not null)
        {
            ModelState.AddModelError(nameof(Picture), upload.ErrorMessage);
            LoadTimeline(1, null);
            return Page();
        }

        _service.CreateObservation(author.Id, Text, ObservedTaxonId, upload.PicturePath);

        // Redirect after POST, so refreshing the page does not post twice.
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVoteAsync(
        int postId,
        bool direction,
        int page = 1,
        string? taxonId = null)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        var author = await _userManager.GetUserAsync(User);
        if (author is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        try
        {
            if (direction)
            {
                if (_voteService.HasVotedPost(postId, author.Id, direction))
                {
                    _voteService.RemoveUpvote(postId, author.Id);
                }
                else if (_voteService.HasVotedPost(postId, author.Id, !direction))
                {
                    TempData["VoteError"] = "You have already voted on this post in the opposite direction.";
                }
                else
                {
                    _voteService.Upvote(postId, author.Id);
                }
            }
            else
            {
                if (_voteService.HasVotedPost(postId, author.Id, direction))
                {
                    _voteService.RemoveDownvote(postId, author.Id);
                }
                else if (_voteService.HasVotedPost(postId, author.Id, !direction))
                {
                    TempData["VoteError"] = "You have already voted on this post in the opposite direction.";
                }
                else
                {
                    _voteService.Downvote(postId, author.Id);
                }
            }
        }
        catch (ArgumentException)
        {
            TempData["VoteError"] = "You have already voted on this post, or the post could not be found.";
        }
        catch (DbUpdateException)
        {
            TempData["VoteError"] = "Your vote could not be saved. Please try again.";
        }

        return RedirectToPage("/Public", new { page = Math.Max(page, 1), taxonId });
    }

    private void LoadTimeline(int page, string? taxonId)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;
        SelectedTaxonId = taxonId;
        Taxa = _taxonService.GetTaxa();

        var result = string.IsNullOrWhiteSpace(taxonId)
            ? _service.GetObservations(page)
            : _service.GetObservationsForTaxon(taxonId, page);

        Observations = result.Observations;
        HasNextPage = result.HasNextPage;
    }
}
