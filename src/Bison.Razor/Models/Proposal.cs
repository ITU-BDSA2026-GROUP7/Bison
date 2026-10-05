namespace Bison.Razor.Models;

public class Proposal : Post
{
    public int ObservationId { get; set; }
    public Observation Observation { get; set; } = null!;

    public string TaxonId { get; set; } = string.Empty;
    public Taxon Taxon { get; set; } = null!;
}
