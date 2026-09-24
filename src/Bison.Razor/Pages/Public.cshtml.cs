using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly IObservationService _service;

    public List<ObservationViewModel> Observations { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }

    public PublicModel(IObservationService service)
    {
        _service = service;
    }

    public ActionResult OnGet([FromQuery] int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;

        var result = _service.GetObservations(page);

        Observations = result.Observations;
        HasNextPage = result.HasNextPage;

        return Page();
    }
}