using Bison.Core.DTO;
using Bison.Core.Domain;
using Bison.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;


namespace Bison.Infrastructure.Repositories;

public class AuthorRepository : IAuthorRepository
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm";

    private readonly BisonDBContext _context;

        public List<string> ForgetAuthor(int authorId, bool keepObservations)
    {
        // AsNoTracking: we delete with direct SQL below, so a tracked copy would go stale.
        var author = _context.Authors.AsNoTracking().FirstOrDefault(a => a.Id == authorId);
        if (author == null)
        {
            throw new ArgumentException($"No author with id {authorId} exists.", nameof(authorId));
        }

        if (author.UserName == DeletedUser.UserName)
        {
            throw new InvalidOperationException("The 'deleted user' placeholder cannot be forgotten.");
        }

        // All or nothing: a half-forgotten user would be worse than none.
        using var transaction = _context.Database.BeginTransaction();

        var deletedUserId = GetOrCreateDeletedUserId();
        var picturePaths = new List<string>();

        // Comments and proposals are anonymized by deletion, wherever they were written.
        _context.Comments.Where(c => c.AuthorId == authorId).ExecuteDelete();
        _context.Proposals.Where(p => p.AuthorId == authorId).ExecuteDelete();

        if (keepObservations)
        {
            // Disconnect the observations from the user.
            _context.Observations
                .Where(o => o.AuthorId == authorId)
                .ExecuteUpdate(s => s.SetProperty(o => o.AuthorId, deletedUserId));
        }
        else
        {
            var observationIds = _context.Observations
                .Where(o => o.AuthorId == authorId)
                .Select(o => o.Id);

            picturePaths = _context.Observations
                .Where(o => o.AuthorId == authorId && o.PicturePath != null)
                .Select(o => o.PicturePath!)
                .ToList();

            // Other people's comments and proposals disappear with the observation.
            _context.Comments.Where(c => observationIds.Contains(c.ObservationId)).ExecuteDelete();
            _context.Proposals.Where(p => observationIds.Contains(p.ObservationId)).ExecuteDelete();
            _context.Observations.Where(o => o.AuthorId == authorId).ExecuteDelete();
        }

        // The login data and finally the user row itself (name, e-mail, password hash ...).
        _context.UserLogins.Where(l => l.UserId == authorId).ExecuteDelete();
        _context.UserClaims.Where(c => c.UserId == authorId).ExecuteDelete();
        _context.UserTokens.Where(t => t.UserId == authorId).ExecuteDelete();
        _context.UserRoles.Where(r => r.UserId == authorId).ExecuteDelete();
        _context.Authors.Where(a => a.Id == authorId).ExecuteDelete();

        transaction.Commit();
        return picturePaths;
    }

    // The one shared author that kept observations are handed to.
    // It has no password and is locked out forever, so nobody can log in as it.
    private int GetOrCreateDeletedUserId()
    {
        var existingId = _context.Authors
            .Where(a => a.UserName == DeletedUser.UserName)
            .Select(a => (int?)a.Id)
            .FirstOrDefault();

        if (existingId != null)
        {
            return existingId.Value;
        }

        var placeholder = new Author
        {
            Name = DeletedUser.Name,
            UserName = DeletedUser.UserName,
            NormalizedUserName = DeletedUser.UserName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.MaxValue
        };

        _context.Authors.Add(placeholder);
        _context.SaveChanges();
        return placeholder.Id;
    }

    public AuthorRepository(BisonDBContext context)
    {
        _context = context;
    }

    public UserInfoDTO? GetUserInfo(int authorId)
    {
        // Look the user up by id, not by display name: names need not be unique.
        var author = _context.Authors.FirstOrDefault(a => a.Id == authorId);
        if (author == null)
        {
            return null;
        }

        return new UserInfoDTO
        {
            Id = author.Id,
            Name = author.Name,
            UserName = author.UserName ?? "",
            Email = author.Email,
            EmailConfirmed = author.EmailConfirmed,
            PhoneNumber = author.PhoneNumber,
            HasPassword = author.PasswordHash != null,

            // AspNetUserLogins holds one row per external login (e.g. GitHub).
            ExternalLogins = _context.UserLogins
                .Where(l => l.UserId == authorId)
                .Select(l => l.ProviderDisplayName ?? l.LoginProvider)
                .ToList(),

            Observations = _context.Observations
                .Where(o => o.AuthorId == authorId)
                .OrderByDescending(o => o.TimeStamp)
                .Select(o => new ObservationDTO
                {
                    Id = o.Id,
                    Text = o.Text,
                    AuthorName = author.Name,
                    TaxonName = o.Taxon != null ? o.Taxon.DanishVernacularName : "",
                    Timestamp = o.TimeStamp.ToString(TimestampFormat),
                    PicturePath = o.PicturePath
                })
                .ToList(),

            Comments = _context.Comments
                .Where(c => c.AuthorId == authorId)
                .OrderByDescending(c => c.TimeStamp)
                .Select(c => new CommentDTO
                {
                    Id = c.Id,
                    ObservationId = c.ObservationId,
                    Text = c.Text,
                    Timestamp = c.TimeStamp.ToString(TimestampFormat)
                })
                .ToList(),

            Proposals = _context.Proposals
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.TimeStamp)
                .Select(p => new ProposalDTO
                {
                    Id = p.Id,
                    ObservationId = p.ObservationId,
                    TaxonName = p.Taxon.DanishVernacularName,
                    Text = p.Text,
                    Timestamp = p.TimeStamp.ToString(TimestampFormat)
                })
                .ToList(),

            Votes = _context.Votes
                .Where(v => v.AuthorId == authorId)
                .OrderByDescending(v => v.TimeStamp)
                .Select(v => new VoteDTO
                {
                    Id = v.Id,
                    PostId = v.PostId,
                    Direction = v.Direction,
                    Timestamp = v.TimeStamp.ToString(TimestampFormat)
                })
                .ToList(),
        };
    }
}
