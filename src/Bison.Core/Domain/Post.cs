namespace Bison.Core.Domain;

public abstract class Post : AuthoredEntity
{
    public string Text { get; set; } = string.Empty;

    public int UpVotes { get; set; }

    public int DownVotes { get; set; }
}
