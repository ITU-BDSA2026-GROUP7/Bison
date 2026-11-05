namespace Bison.Core.Domain;

public class Comment : Post
{
    public int ObservationId { get; set; }
    public Observation Observation { get; set; } = null!;
}
