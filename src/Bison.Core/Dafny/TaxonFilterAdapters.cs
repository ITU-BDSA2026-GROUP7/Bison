using Bison.Core.DTO;

namespace TaxonFilter;

public class Taxon
{
    private readonly TaxonDTO _taxon;
    private readonly IReadOnlyDictionary<string, TaxonDTO> _taxaById;

    public Taxon(TaxonDTO taxon, IReadOnlyDictionary<string, TaxonDTO> taxaById)
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
    private readonly ObservationDTO _observation;
    public ObservationDTO Original => _observation;
    private readonly IReadOnlyDictionary<string, TaxonDTO> _taxaById;

    public Observation(ObservationDTO observation, IReadOnlyDictionary<string, TaxonDTO> taxaById)
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
