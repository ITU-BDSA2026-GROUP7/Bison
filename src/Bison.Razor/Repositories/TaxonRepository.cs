using Bison.Razor.Models;
using Bison.Razor.DTO;

namespace Bison.Razor.Repositories;

public interface ITaxonRepository {

    public List<TaxonDTO> GetTaxa();
}

public class TaxonRepository : ITaxonRepository
{
    private readonly BisonDBContext _context;

    public TaxonRepository(BisonDBContext context)
    {
        _context = context;
    }

    public List<TaxonDTO> GetTaxa()
    {
       return _context.Taxa
        .OrderBy(t => t.DanishVernacularName)
        .Select(t => new TaxonDTO
        {
            DwcTaxonId = t.DwcTaxonId,
            DanishVernacularName = t.DanishVernacularName,
            ParentId = t.ParentId
        })
        .ToList();
    }
}