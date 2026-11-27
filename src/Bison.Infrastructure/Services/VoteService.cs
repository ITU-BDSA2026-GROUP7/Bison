using Bison.Infrastructure.Repositories;

namespace Bison.Infrastructure.Services;

public interface IVoteService
{
    void Upvote(int postId, int authorId);
    void Downvote(int postId, int authorId);
    void RemoveUpvote(int postId, int authorId);
    void RemoveDownvote(int postId, int authorId);
    bool HasVotedPost(int postId, int authorId, bool direction);
}

public class VoteService : IVoteService
{
    private readonly IVoteRepository _repository;

    public VoteService(IVoteRepository repository)
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

    public void RemoveUpvote(int postId, int authorId)
    {
        _repository.RemoveVote(postId, authorId, true);
    }

    public void RemoveDownvote(int postId, int authorId)
    {
        _repository.RemoveVote(postId, authorId, false);
    }
    
    public bool HasVotedPost(int postId, int authorId, bool direction)
    {
        return _repository.HasVotedPost(postId, authorId, direction);
    }

}
