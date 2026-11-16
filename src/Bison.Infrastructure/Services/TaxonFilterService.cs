using Bison.Core.DTO;
using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public interface ITaxonFilterService
{
    List<ObservationDTO> FilterObservations(string taxonId);
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

    public List<ObservationDTO> FilterObservations(string taxonId)
    {
        // Load all taxa so we can connect the complete hierarchy.
        var taxa = _taxonRepository.GetTaxa();

        var taxaById = taxa.ToDictionary(t => t.DwcTaxonId);

        // Find the taxon the user selected.
        if (!taxaById.TryGetValue(taxonId, out var selectedTaxon))
        {
            return new List<ObservationDTO>();
        }

        // Load observations with taxon IDs for the Dafny filter.
        var observations = _postRepository.GetObservationsForTaxonFilter();

        // Wrap the selected taxon and observations for the generated Dafny code.
        var dafnyRoot = new TaxonFilter.Taxon(selectedTaxon, taxaById);

        var dafnyObservations = observations
            .Where(o => o.TaxonId != null && taxaById.ContainsKey(o.TaxonId))
            .Select(o => new TaxonFilter.Observation(o, taxaById))
            .ToList();

        var sequence =
            Dafny.Sequence<TaxonFilter.Observation>.FromElements(
                dafnyObservations.ToArray());

        // Run the Dafny-generated filter.
        var filtered = FilterWrapper.__default.Filter(
            dafnyRoot,
            sequence);

        return filtered
            .Select(o => o.Original)
            .ToList();
    }
}
