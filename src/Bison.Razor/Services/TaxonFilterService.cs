
using Bison.Razor.Models;
using Bison.Razor.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Services;

public interface ITaxonFilterService
{
    List<Observation> FilterObservations(string taxonId);
}

public class TaxonFilterService : ITaxonFilterService
{
    private readonly IPostRepository _postRepository;
    private readonly ITaxonRepository _taxonRepository;

    public TaxonFilterService(
        IPostRepository postRepository,
        ITaxonRepository taxonRepository)
    {
        _postRepository = postRepository;
        _taxonRepository = taxonRepository;
    }

    public List<Observation> FilterObservations(string taxonId)
    {
        // Load all taxa so we can connect the complete hierarchy.
        var taxa = _taxonRepository.GetTaxa().ToList();

        var taxaById = taxa.ToDictionary(t => t.DwcTaxonId);

        foreach (var taxon in taxa)
        {
            if (taxon.ParentId != null &&
                taxaById.TryGetValue(taxon.ParentId, out var parent))
            {
                taxon.Parent = parent;
            }
            else
            {
                taxon.Parent = null;
            }
        }

        // Find the taxon the user selected.
        if (!taxaById.TryGetValue(taxonId, out var selectedTaxon))
        {
            return new List<Observation>();
        }

        // Load observations, including their taxa.
        var observations = _postRepository.GetObservationsForTaxonFilter();

        // Wrap the selected taxon and observations for the generated Dafny code.
        var dafnyRoot = new TaxonFilter.Taxon(selectedTaxon);

        var dafnyObservations = observations
            .Where(o => o.Taxon != null)
            .Select(o => new TaxonFilter.Observation(o))
            .ToList();

        var sequence =
            Dafny.Sequence<TaxonFilter.Observation>.FromElements(
                dafnyObservations.ToArray());

        // Run the Dafny-generated filter.
        var filtered = FilterWrapper.__default.Filter(
            dafnyRoot,
            sequence);

        // Convert the result back into the original application models.
        return filtered
            .Select(o => o.Original)
            .ToList();
    }
}
