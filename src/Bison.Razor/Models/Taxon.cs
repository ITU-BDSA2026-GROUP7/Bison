namespace Bison.Razor.Models;

public class Taxon
{
    public string DwcTaxonId { get; set; } = string.Empty;

    public string DanishVernacularName { get; set; } = string.Empty;

    public string? ParentId { get; set; }
    public Taxon? Parent { get; set; }

    public List<Taxon> Children { get; set; } = new();
}
