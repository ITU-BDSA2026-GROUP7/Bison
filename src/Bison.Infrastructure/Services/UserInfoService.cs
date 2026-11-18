using Bison.Core.DTO;
using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public interface IUserInfoService
{
    UserInfoDTO? GetUserInfo(int authorId);
}

// Thin for now, but the "Forget me!" feature and the optional
// ZIP download will build on this service.
public class UserInfoService : IUserInfoService
{
    private readonly IAuthorRepository _repository;

    public UserInfoService(IAuthorRepository repository)
    {
        _repository = repository;
    }

    public UserInfoDTO? GetUserInfo(int authorId)
    {
        return _repository.GetUserInfo(authorId);
    }
}
