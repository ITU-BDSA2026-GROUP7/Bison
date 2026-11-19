using Bison.Core.DTO;

namespace Bison.Infrastructure.Repositories;

public interface IAuthorRepository
{
    // Collects all data stored about the author with the given id.
    // Returns null if no such author exists.
    UserInfoDTO? GetUserInfo(int authorId);

        // "Forget me": removes everything stored about the author.
    // Comments and proposals are always deleted. Observations are either kept
    // (handed to the shared "deleted user") or deleted together with the
    // comments and proposals others wrote on them.
    // Returns the picture paths of deleted observations, so the files can be removed.
    List<string> ForgetAuthor(int authorId, bool keepObservations);
}


