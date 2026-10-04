using Bison.Razor.Models;

namespace Bison.Razor.Repositories;

public interface IPostRepository
{
    List<Observation> GetObservations(int skip, int take);
    List<Observation> GetObservationsFromAuthor(string author, int skip, int take);
    Observation? GetObservation(int id);

    List<Comment> GetComments(int observationId, int skip, int take);
    List<Proposal> GetProposals(int observationId, int skip, int take);
}