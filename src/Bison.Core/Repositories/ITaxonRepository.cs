using Bison.Core.DTO;

namespace Bison.Core.Repositories;

public interface ITaxonRepository
{
    List<TaxonDTO> GetTaxa();
}
