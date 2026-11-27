namespace Bison.Core.Domain;

public abstract class AuthoredEntity
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

}
