using Bison.Core.DTO;

namespace Bison.Infrastructure.Repositories;

public interface ITaxonRepository
{
    List<TaxonDTO> GetTaxa();
}
