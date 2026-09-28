using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bison.Razor.Pages;

public class ObservationDetailsModel : PageModel
{
    private readonly IObservationService _observationService;
    private readonly ICommentService _commentService;
    private readonly IProposalService _proposalService;

    public ObservationDetailsPage? Details { get; private set; }

    public ObservationDetailsModel(
        IObservationService observationService,
        ICommentService commentService,
        IProposalService proposalService)
    {
        _observationService = observationService;
        _commentService = commentService;
        _proposalService = proposalService;
    }

    public IActionResult OnGet(int id, [FromQuery] int commentsPage = 1, [FromQuery] int proposalsPage = 1)
    {
        commentsPage = Math.Max(commentsPage, 1);
        proposalsPage = Math.Max(proposalsPage, 1);

        var observation = _observationService.GetObservation(id);

        if (observation == null)
        {
            return NotFound();
        }

        var comments = _commentService.GetComments(id, commentsPage);
        var proposals = _proposalService.GetProposals(id, proposalsPage);

        Details = new ObservationDetailsPage(
            observation,
            comments,
            proposals);

        return Page();
    }
}