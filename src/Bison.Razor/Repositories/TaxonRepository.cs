using Bison.Razor.Models;

namespace Bison.Razor.Repositories;

public interface ITaxonRepository {

    public List<Taxon> GetTaxa();
}

public class TaxonRepository : ITaxonRepository
{
    private readonly BisonDBContext _context;

    public TaxonRepository(BisonDBContext context)
    {
        _context = context;
    }

    public List<Taxon> GetTaxa()
    {
        return _context.Taxa
            .OrderBy(t => t.DanishVernacularName)
            .ToList();
    }
}