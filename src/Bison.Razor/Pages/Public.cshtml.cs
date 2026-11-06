using System.ComponentModel.DataAnnotations;
using Bison.Core.Domain;
using Bison.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly IObservationService _service;

    private readonly ITaxonService _taxonService;

    private readonly UserManager<Author> _userManager;

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

    public PublicModel(
        IObservationService service,
        ITaxonService taxonService,
        UserManager<Author> userManager)
    {
        _service = service;
        _taxonService = taxonService;
        _userManager = userManager;
    }

    public ActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] string? taxonId = null)
    {
        LoadTimeline(page, taxonId);
        return Page();
    }

    public ActionResult OnPost()
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

        var authorId = int.Parse(_userManager.GetUserId(User)!);
        _service.CreateObservation(authorId, Text, ObservedTaxonId);

        // Redirect after POST, so refreshing the page does not post twice.
        return RedirectToPage();
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
