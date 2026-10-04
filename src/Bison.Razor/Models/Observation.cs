namespace Bison.Razor.Models;

public class Observation : Post
{
    public string? TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    public List<Comment> Comments { get; set; } = new();
    public List<Proposal> Proposals { get; set; } = new();
}
