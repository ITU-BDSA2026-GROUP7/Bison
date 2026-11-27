namespace Bison.Core.DTO;

public class VoteDTO : AuthoredEntityDTO
{
    public int PostId { get; set; }
    public bool Direction { get; set; }
}