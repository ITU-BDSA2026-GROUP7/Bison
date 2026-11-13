using Bison.Core.DTO;
using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public record TaxonViewModel(string DwcTaxonId, string DanishVernacularName);

public interface ITaxonService
{
    public List<TaxonViewModel> GetTaxa();
}

public class TaxonService : ITaxonService
{
    private readonly ITaxonRepository _taxonRepository;

    public TaxonService(ITaxonRepository taxonRepository)
    {
        _taxonRepository = taxonRepository;
    }

    public List<TaxonViewModel> GetTaxa()
    {
        var list = new List<TaxonViewModel>();
        var taxa = _taxonRepository.GetTaxa();
        
        foreach (TaxonDTO taxon in taxa) {
            var taxonmodel = new TaxonViewModel(taxon.DwcTaxonId,
            taxon.DanishVernacularName);
            
            list.Add(taxonmodel);
        }

        return list;
    }
}
