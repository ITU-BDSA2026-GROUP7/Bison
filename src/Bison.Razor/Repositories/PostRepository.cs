using Bison.Razor.Models;
using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Repositories;

public class PostRepository : IPostRepository
{
    private readonly BisonDBContext _context;

    public PostRepository(BisonDBContext context)
    {
        _context = context;
    }

    public List<Observation> GetObservations(int skip, int take)
    {
        return _context.Observations
            .Include(o => o.Author)
            .OrderByDescending(o => o.TimeStamp)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public List<Observation> GetObservationsFromAuthor(string author, int skip, int take)
    {
        return _context.Observations
            .Include(o => o.Author)
            .Where(o => o.Author.Name == author)
            .OrderByDescending(o => o.TimeStamp)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public Observation? GetObservation(int id)
    {
        return _context.Observations
            .Include(o => o.Author)
            .FirstOrDefault(o => o.Id == id);
    }

    public List<Comment> GetComments(int observationId, int skip, int take)
    {
        return _context.Comments
            .Include(c => c.Author)
            .Where(c => c.ObservationId == observationId)
            .OrderByDescending(c => c.TimeStamp)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public List<Proposal> GetProposals(int observationId, int skip, int take)
    {
        return _context.Proposals
            .Include(p => p.Author)
            .Where(p => p.ObservationId == observationId)
            .OrderByDescending(p => p.TimeStamp)
            .Skip(skip)
            .Take(take)
            .ToList();
    }
}