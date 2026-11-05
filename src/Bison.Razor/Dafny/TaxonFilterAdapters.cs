namespace TaxonFilter;

public class Taxon
{
    private readonly Bison.Razor.DTO.TaxonDTO _taxon;
    private readonly IReadOnlyDictionary<string, Bison.Razor.DTO.TaxonDTO> _taxaById;

    public Taxon(Bison.Razor.DTO.TaxonDTO taxon, IReadOnlyDictionary<string, Bison.Razor.DTO.TaxonDTO> taxaById)
    {
        _taxon = taxon;
        _taxaById = taxaById;
    }

    public bool isSubTaxon(Taxon ancestor)
    {
        var current = _taxon;

    while (true)
    {
        if (current.DwcTaxonId == ancestor._taxon.DwcTaxonId)
            return true;

        if (current.ParentId is null ||
            !_taxaById.TryGetValue(current.ParentId, out current))
            return false;
    }

    }

    public string getId()
    {
        return _taxon.DwcTaxonId;
    }
}

public class Observation
{
    private readonly Bison.Razor.DTO.ObservationDTO _observation;
    public Bison.Razor.DTO.ObservationDTO Original => _observation;
    private readonly IReadOnlyDictionary<string, Bison.Razor.DTO.TaxonDTO> _taxaById;

    public Observation(Bison.Razor.DTO.ObservationDTO observation, IReadOnlyDictionary<string, Bison.Razor.DTO.TaxonDTO> taxaById)
    {
        _observation = observation;
        _taxaById = taxaById;

    }

    public Taxon getTaxon() 
    {
        if (_observation.TaxonId == null ||
            !_taxaById.TryGetValue(_observation.TaxonId, out var taxon))
        {
            throw new InvalidOperationException(
                "Cannot filter an observation without a matching taxon.");
        }

        return new Taxon(taxon, _taxaById);
    }
}
