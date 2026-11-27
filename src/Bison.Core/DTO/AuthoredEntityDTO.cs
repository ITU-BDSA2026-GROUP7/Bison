namespace Bison.Core.DTO;
public abstract class AuthoredEntityDTO
{
    public int Id { get; set; }
    public string Timestamp { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
}
