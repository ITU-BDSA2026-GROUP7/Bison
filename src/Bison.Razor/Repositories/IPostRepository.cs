using Bison.Razor.Models;

namespace Bison.Razor.Repositories;

public interface IPostRepository
{
    public List<ObservationListDTO> GetObservations(int skip, int take);


    List<ObservationListDTO> GetObservationsFromAuthor(string author, int skip, int take);
    ObservationDetailsDTO? GetObservation(int id);

    List<CommentDTO> GetComments(int observationId, int skip, int take);
    List<ProposalDTO> GetProposals(int observationId, int skip, int take);
}