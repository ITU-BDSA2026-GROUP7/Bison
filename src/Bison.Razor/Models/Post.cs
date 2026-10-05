namespace Bison.Razor.Models;

public abstract class Post
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime TimeStamp { get; set; }

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}
