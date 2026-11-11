using Bison.Core.Domain;
using Bison.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class ObservationDetailsModel : PageModel
{
    private readonly IObservationService _observationService;
    private readonly ICommentService _commentService;
    private readonly IProposalService _proposalService;
    private readonly ITaxonService _taxonService;
    private readonly UserManager<Author> _userManager;
    private readonly SignInManager<Author> _signInManager;

    public ObservationDetailsPage? Details { get; private set; }

    // The taxa offered in the proposal dropdown. Genera without a Danish name
    // are left out, exactly like in the "What did you see?" dropdown.
    public List<TaxonViewModel> NamedTaxa { get; private set; } = new();

    // Filled from the comment form. Nullable on purpose: a non-nullable string
    // would get an automatic "required" error even when the OTHER form is posted.
    [BindProperty]
    public string? CommentText { get; set; }

    // Filled from the proposal form.
    [BindProperty]
    public string? ProposalText { get; set; }

    [BindProperty]
    public string? ProposedTaxonId { get; set; }

    public ObservationDetailsModel(
        IObservationService observationService,
        ICommentService commentService,
        IProposalService proposalService,
        ITaxonService taxonService,
        UserManager<Author> userManager,
        SignInManager<Author> signInManager)
    {
        _observationService = observationService;
        _commentService = commentService;
        _proposalService = proposalService;
        _taxonService = taxonService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult OnGet(int id, [FromQuery] int commentsPage = 1, [FromQuery] int proposalsPage = 1)
    {
        return LoadDetails(id, commentsPage, proposalsPage) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostComment(int id)
    {
        // Only logged-in users may comment.
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (_observationService.GetObservation(id) == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(CommentText))
        {
            ModelState.AddModelError(nameof(CommentText), "Write a comment before sharing.");
        }

        if (!ModelState.IsValid)
        {
            // Show the page again, with the error next to the form.
            LoadDetails(id);
            return Page();
        }

        var author = await _userManager.GetUserAsync(User);
        if (author is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        _commentService.CreateComment(author.Id, id, CommentText!);

        // Redirect after POST, so refreshing the page does not post twice.
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostProposal(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (_observationService.GetObservation(id) == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(ProposalText))
        {
            ModelState.AddModelError(nameof(ProposalText), "Explain your proposal before sharing.");
        }

        // A proposal must name a real taxon. The dropdown only offers real ones,
        // but a hand-made request could send anything.
        if (string.IsNullOrEmpty(ProposedTaxonId) ||
            !_taxonService.GetTaxa().Any(t => t.DwcTaxonId == ProposedTaxonId))
        {
            ModelState.AddModelError(nameof(ProposedTaxonId), "Pick a taxon from the list.");
        }

        if (!ModelState.IsValid)
        {
            LoadDetails(id);
            return Page();
        }

        var author = await _userManager.GetUserAsync(User);
        if (author is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        _proposalService.CreateProposal(author.Id, id, ProposedTaxonId!, ProposalText!);

        return RedirectToPage(new { id });
    }

    // Shared by OnGet and by the handlers that show the page again after an error.
    private bool LoadDetails(int id, int commentsPage = 1, int proposalsPage = 1)
    {
        commentsPage = Math.Max(commentsPage, 1);
        proposalsPage = Math.Max(proposalsPage, 1);

        var observation = _observationService.GetObservation(id);

        if (observation == null)
        {
            return false;
        }

        var comments = _commentService.GetComments(id, commentsPage);
        var proposals = _proposalService.GetProposals(id, proposalsPage);

        Details = new ObservationDetailsPage(
            observation,
            comments,
            proposals);

        // Only logged-in users see the proposal form, so only they need the list.
        if (User.Identity?.IsAuthenticated == true)
        {
            NamedTaxa = _taxonService.GetTaxa()
                .Where(t => !string.IsNullOrEmpty(t.DanishVernacularName))
                .ToList();
        }

        return true;
    }
}