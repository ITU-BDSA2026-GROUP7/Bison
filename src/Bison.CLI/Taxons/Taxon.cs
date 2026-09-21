//Use CsvHelper instead...

public class Taxon{
    public String TaxonId { get; set;} = "";
    public String ParentTaxonId {get; set;} = "";
    public String Rank {get; set;} = "";
    public String ScientificName {get; set;} = "";
    public String? VernacularName {get; set;} //? allows null
}