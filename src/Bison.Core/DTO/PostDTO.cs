namespace Bison.Core.DTO;

public abstract class PostDTO : AuthoredEntityDTO
{
    public string Text { get; set; } = "";
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
}