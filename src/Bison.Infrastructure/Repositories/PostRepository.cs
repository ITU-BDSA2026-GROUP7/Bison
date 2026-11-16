using Bison.Core.Domain;
using Bison.Core.DTO;
using Microsoft.EntityFrameworkCore;
using Bison.Infrastructure.Persistence;

namespace Bison.Infrastructure.Repositories;

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
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                PicturePath = o.PicturePath
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
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                PicturePath = o.PicturePath
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
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                PicturePath = o.PicturePath
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

    public List<ObservationDTO> GetObservationsForTaxonFilter()
    {
        return _context.Observations
            .OrderByDescending(o => o.TimeStamp)
            .Select(o => new ObservationDTO
            {
                Id = o.Id,
                AuthorName = o.Author.Name,
                Text = o.Text,
                Timestamp = o.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                TaxonId = o.TaxonId,
                PicturePath = o.PicturePath
            })
            .ToList();
    }

    public int CreateObservation(int authorId, string text, string? taxonId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("An observation must have text.", nameof(text));
        }

        // Fail with a clear message instead of a foreign-key error from SQLite.
        if (!_context.Authors.Any(a => a.Id == authorId))
        {
            throw new ArgumentException($"No author with id {authorId} exists.", nameof(authorId));
        }

        // An empty choice means "Don't know", which is stored as no taxon.
        if (string.IsNullOrWhiteSpace(taxonId))
        {
            taxonId = null;
        }
        else if (!_context.Taxa.Any(t => t.DwcTaxonId == taxonId))
        {
            throw new ArgumentException($"No taxon with id {taxonId} exists.", nameof(taxonId));
        }

        var observation = new Observation
        {
            AuthorId = authorId,
            Text = text.Trim(),
            TaxonId = taxonId,
            TimeStamp = DateTime.UtcNow
        };

        _context.Observations.Add(observation);
        _context.SaveChanges();

        // EF Core fills in the generated Id when SaveChanges runs.
        return observation.Id;
    }

        public int CreateComment(int authorId, int observationId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A comment must have text.", nameof(text));
        }

        EnsureAuthorExists(authorId);
        EnsureObservationExists(observationId);

        var comment = new Comment
        {
            AuthorId = authorId,
            ObservationId = observationId,
            Text = text.Trim(),
            TimeStamp = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        _context.SaveChanges();

        return comment.Id;
    }

    public int CreateProposal(int authorId, int observationId, string taxonId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A proposal must have text.", nameof(text));
        }

        // Proposal.TaxonId is not nullable: a proposal is a suggestion of a species.
        if (string.IsNullOrWhiteSpace(taxonId))
        {
            throw new ArgumentException("A proposal must name a taxon.", nameof(taxonId));
        }

        EnsureAuthorExists(authorId);
        EnsureObservationExists(observationId);

        if (!_context.Taxa.Any(t => t.DwcTaxonId == taxonId))
        {
            throw new ArgumentException($"No taxon with id {taxonId} exists.", nameof(taxonId));
        }

        var proposal = new Proposal
        {
            AuthorId = authorId,
            ObservationId = observationId,
            TaxonId = taxonId,
            Text = text.Trim(),
            TimeStamp = DateTime.UtcNow
        };

        _context.Proposals.Add(proposal);
        _context.SaveChanges();

        return proposal.Id;
    }

    // Fail with a clear message instead of a foreign-key error from SQLite.
    private void EnsureAuthorExists(int authorId)
    {
        if (!_context.Authors.Any(a => a.Id == authorId))
        {
            throw new ArgumentException($"No author with id {authorId} exists.", nameof(authorId));
        }
    }

    private void EnsureObservationExists(int observationId)
    {
        if (!_context.Observations.Any(o => o.Id == observationId))
        {
            throw new ArgumentException($"No observation with id {observationId} exists.", nameof(observationId));
        }
    }
}
