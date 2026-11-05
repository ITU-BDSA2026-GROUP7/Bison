namespace Bison.Razor.DTO;

public class TaxonDTO
{
    public string DwcTaxonId { get; set; } = "";
    public string DanishVernacularName { get; set; } = "";
    public string? ParentId { get; set; }
}