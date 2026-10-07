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

    public List<ObservationListDTO> GetObservations(int skip, int take)
    {
        return _context.Observations
            .Include(o => o.Author)
            .Include(o => o.Taxon)
            .OrderByDescending(o => o.TimeStamp)
            .Skip(skip)
            .Take(take)
            .Select(o => new ObservationListDTO
            {
                Id = o.Id,
                Text = o.Text,
                AuthorName = o.Author.Name,
                TaxonName = o.Taxon.DanishVernacularName,
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }

    public List<ObservationListDTO> GetObservationsFromAuthor(string author, int skip, int take)
    {
        return _context.Observations
            .Include(o => o.Author)
            .Include(o => o.Taxon)
            .Where(o => o.Author.Name == author)
            .OrderByDescending(o => o.TimeStamp)
            .Skip(skip)
            .Take(take)
            .Select(o => new ObservationListDTO
            {
                Id = o.Id,
                Text = o.Text,
                AuthorName = o.Author.Name,
                TaxonName = o.Taxon.DanishVernacularName,
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }

    public ObservationDetailsDTO? GetObservation(int id)
    {
        return _context.Observations
            .Include(o => o.Author)
            .Include(o => o.Taxon)
            .Where(o => o.Id == id)
            .Select(o => new ObservationDetailsDTO
            {
                Id = o.Id,
                Text = o.Text,
                AuthorName = o.Author.Name,
                TaxonName = o.Taxon.DanishVernacularName,
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm")
            })
            .FirstOrDefault();
    }

    public List<CommentDTO> GetComments(int observationId, int skip, int take)
    {
        return _context.Comments
            .Include(c => c.Author)
            .Where(c => c.ObservationId == observationId)
            .OrderByDescending(c => c.TimeStamp)
            .Skip(skip)
            .Take(take)
            .Select(c => new CommentDTO
            {
                Id = c.Id,
                Text = c.Text,
                AuthorName = c.Author.Name,
                Timestamp = c.TimeStamp.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }

    public List<ProposalDTO> GetProposals(int observationId, int skip, int take)
    {
        return _context.Proposals
            .Include(p => p.Author)
            .Include(p => p.Taxon)
            .Where(p => p.ObservationId == observationId)
            .OrderByDescending(p => p.TimeStamp)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProposalDTO
            {
                Id = p.Id,
                Text = p.Text,
                AuthorName = p.Author.Name,
                TaxonName = p.Taxon.DanishVernacularName,
                Timestamp = p.TimeStamp.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }
}