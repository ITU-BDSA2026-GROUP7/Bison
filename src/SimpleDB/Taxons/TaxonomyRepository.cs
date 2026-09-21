public class TaxonomyRepository{
    //readonly makes objects more immutable by ensuring certain values never change af construction
    private readonly Dictionary<string, Taxon> taxonsById = new();

    public void Add(Taxon taxon){

        taxonsById[taxon.TaxonId] = taxon;

    }

    public Taxon? GetById(string taxonId){
        taxonsById.TryGetValue(taxonId, out var taxon);
        return taxon;
    }

    public Taxon? GetByVernacularName(string name){
        return taxonsById.Values.FirstOrDefault(
            t => !string.IsNullOrEmpty(t.VernacularName)
             && t.VernacularName.Equals(name, StringComparison.OrdinalIgnoreCase));

    }

    public Taxon? GetParent(string taxonId){
        var taxon = GetById(taxonId);

        if (taxon == null){
            return null;
        }
        return GetById(taxon.ParentTaxonId);
    }

    public IEnumerable<Taxon> GetChildren(string taxonId){
        return taxonsById.Values.Where(t => t.ParentTaxonId == taxonId);
    }

    public int Count(){
        return taxonsById.Count;
    }
}