using Bison.Core.DTO;
using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public interface IUserInfoService
{
    UserInfoDTO? GetUserInfo(int authorId);

    // "Forget me": see IAuthorRepository.ForgetAuthor. Also deletes the picture
    // files of observations that were deleted.
    void ForgetUser(int authorId, bool keepObservations);
}

public class UserInfoService : IUserInfoService
{
    private readonly IAuthorRepository _repository;
    private readonly ObservationPictureStorage _pictureStorage;

    public UserInfoService(IAuthorRepository repository, ObservationPictureStorage pictureStorage)
    {
        _repository = repository;
        _pictureStorage = pictureStorage;
    }

    public UserInfoDTO? GetUserInfo(int authorId)
    {
        return _repository.GetUserInfo(authorId);
    }

    public void ForgetUser(int authorId, bool keepObservations)
    {
        // Database first: if it fails, no picture has been deleted yet.
        var picturePaths = _repository.ForgetAuthor(authorId, keepObservations);

        foreach (var path in picturePaths)
        {
            _pictureStorage.Delete(path);
        }
    }
}