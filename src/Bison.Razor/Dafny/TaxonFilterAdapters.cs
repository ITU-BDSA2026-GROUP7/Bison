using Bison.Razor.Models;

namespace TaxonFilter;

public class Taxon
{
    private readonly Bison.Razor.Models.Taxon _taxon;

    public Taxon(Bison.Razor.Models.Taxon taxon)
    {
        _taxon = taxon;
    }

    public bool isSubTaxon(Taxon ancester)
    {
        var current = _taxon;

        while (current != null)
        {
            if (current.DwcTaxonId == ancester._taxon.DwcTaxonId)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}

public class Observation
{
    private readonly Bison.Razor.Models.Observation _observation;
    public Bison.Razor.Models.Observation Original => _observation;

    public Observation(Bison.Razor.Models.Observation observation)
    {
        _observation = observation;
    }

    public Taxon getTaxon()
    {
        if (_observation.Taxon == null)
        {
            throw new InvalidOperationException(
                "Cannot filter an observation without a taxon.");
        }

        return new Taxon(_observation.Taxon);
    }
}
