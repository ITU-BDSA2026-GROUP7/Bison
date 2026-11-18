using Bison.Core.DTO;

namespace Bison.Infrastructure.Repositories;

public interface IAuthorRepository
{
    // Collects all data stored about the author with the given id.
    // Returns null if no such author exists.
    UserInfoDTO? GetUserInfo(int authorId);
}
