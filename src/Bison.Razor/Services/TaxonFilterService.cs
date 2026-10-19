
using Bison.Razor.Models;
using Bison.Razor.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Services;

public interface ITaxonFilterService
{
    List<Observation> FilterObservations(string taxonId);

    List<Taxon> GetTaxa();
}

public class TaxonFilterService : ITaxonFilterService
{
    private readonly IPostRepository _postRepository;
    private readonly BisonDBContext _context;

    public TaxonFilterService(
        IPostRepository postRepository,
        BisonDBContext context)
    {
        _postRepository = postRepository;
        _context = context;
    }

    public List<Observation> FilterObservations(string taxonId)
    {
        // Load all taxa so we can connect the complete hierarchy.
        var taxa = _context.Taxa.ToList();

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

    public List<Taxon> GetTaxa()
    {
        return _context.Taxa
            .OrderBy(t => t.DanishVernacularName)
            .ToList();
    }
}
