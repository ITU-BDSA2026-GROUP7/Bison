
namespace Bison.Core.DTO;

public class ObservationDTO
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? TaxonId { get; set; }
}
