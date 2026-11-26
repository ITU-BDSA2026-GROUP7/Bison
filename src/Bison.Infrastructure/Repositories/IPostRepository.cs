using System.Data.Common;
using Bison.Core.DTO;

namespace Bison.Infrastructure.Repositories;

public interface IPostRepository
{
    public List<ObservationDTO> GetObservations(int skip, int take);

    List<ObservationDTO> GetObservationsForTaxonFilter();
    List<ObservationDTO> GetObservationsFromAuthor(string author, int skip, int take);

    ObservationDTO? GetObservation(int id);

    List<CommentDTO> GetComments(int observationId, int skip, int take);
    List<ProposalDTO> GetProposals(int observationId, int skip, int take);

    // Saves a new observation written by the author with the given id.
    // taxonId is optional: null or empty means "don't know which species".
    // Returns the id the database gave the new observation.
    int CreateObservation(int authorId, string text, string? taxonId, string? picturePath = null);

    // Saves a new comment on an observation. Returns the id the database gave it.
    int CreateComment(int authorId, int observationId, string text);

    // Saves a new taxon proposal on an observation. Unlike an observation,
    // a proposal must name a taxon. Returns the id the database gave it.
    int CreateProposal(int authorId, int observationId, string taxonId, string text);

    void Vote(int postId, int authorID, bool direction);
}