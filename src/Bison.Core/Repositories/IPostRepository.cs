using Bison.Core.DTO;

namespace Bison.Core.Repositories;

public interface IPostRepository
{
    public List<ObservationListDTO> GetObservations(int skip, int take);

    List<ObservationDTO> GetObservationsForTaxonFilter();
    List<ObservationListDTO> GetObservationsFromAuthor(string author, int skip, int take);
    ObservationDetailsDTO? GetObservation(int id);

    List<CommentDTO> GetComments(int observationId, int skip, int take);
    List<ProposalDTO> GetProposals(int observationId, int skip, int take);

    // Saves a new observation written by the author with the given id.
    // taxonId is optional: null or empty means "don't know which species".
    // Returns the id the database gave the new observation.
    int CreateObservation(int authorId, string text, string? taxonId);
}