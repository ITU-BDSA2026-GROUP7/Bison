namespace Bison.Core.DTO;

// Everything Bison stores about one user, collected for the
// "about me" page (GDPR right of access).
public class UserInfoDTO
{
    // Personal data from the Author / IdentityUser row.
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }

    // We never show the password hash itself, only whether one is stored.
    public bool HasPassword { get; set; }

    // External services the user logs in with, e.g. "GitHub".
    public List<string> ExternalLogins { get; set; } = new();

    // Everything the user has written.
    public List<ObservationDTO> Observations { get; set; } = new();
    public List<CommentDTO> Comments { get; set; } = new();
    public List<ProposalDTO> Proposals { get; set; } = new();
    public List<VoteDTO> Votes { get; set; } = new();
}
