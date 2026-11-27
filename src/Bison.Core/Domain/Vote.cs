namespace Bison.Core.Domain;

public class Vote : AuthoredEntity
{
    public int PostId { get; set; }
    public bool Direction { get; set; }
}