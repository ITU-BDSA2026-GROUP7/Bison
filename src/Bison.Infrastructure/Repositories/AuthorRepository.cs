using Bison.Core.DTO;
using Bison.Infrastructure.Persistence;

namespace Bison.Infrastructure.Repositories;

public class AuthorRepository : IAuthorRepository
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm";

    private readonly BisonDBContext _context;

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
                .Select(o => new ObservationListDTO
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
                .Select(c => new UserCommentDTO
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
                .Select(p => new UserProposalDTO
                {
                    Id = p.Id,
                    ObservationId = p.ObservationId,
                    TaxonName = p.Taxon.DanishVernacularName,
                    Text = p.Text,
                    Timestamp = p.TimeStamp.ToString(TimestampFormat)
                })
                .ToList()
        };
    }
}
