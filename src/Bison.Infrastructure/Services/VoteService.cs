using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public interface IVoteService
{
    void Upvote(int postId, int authorId);
    void Downvote(int postId, int authorId);
}

public class VoteService : IVoteService
{
    private readonly IPostRepository _repository;

    public VoteService(IPostRepository repository)
    {
        _repository = repository;
    }

    public void Upvote(int postId, int authorId)
    {
        _repository.Vote(postId, authorId, true);
    }

    public void Downvote(int postId, int authorId)
    {
        _repository.Vote(postId, authorId, false);
    }
}
