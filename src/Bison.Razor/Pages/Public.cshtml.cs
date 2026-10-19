using Bison.Razor.Models;
using Bison.Razor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly IObservationService _service;

    private readonly ITaxonFilterService _taxonFilterService;

    public List<Taxon> Taxa { get; set; } = new();

    public string? SelectedTaxonId { get; set; }

    public List<ObservationViewModel> Observations { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }

    public PublicModel(
        IObservationService service,
        ITaxonFilterService taxonFilterService)
    {
        _service = service;
        _taxonFilterService = taxonFilterService;
    }

    public ActionResult OnGet(
        [FromQuery] int page = 1,
        [FromQuery] string? taxonId = null)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;
        SelectedTaxonId = taxonId;
        Taxa = _taxonFilterService.GetTaxa();

        var result = string.IsNullOrWhiteSpace(taxonId)
            ? _service.GetObservations(page)
            : _service.GetObservationsForTaxon(taxonId, page);

        Observations = result.Observations;
        HasNextPage = result.HasNextPage;

        return Page();
    }
}