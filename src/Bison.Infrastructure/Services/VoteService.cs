using Bison.Infrastructure.Repositories;

public interface IVoteService {
    public void upvote(int postId, int authorId);
    public void downvote(int postId, int authorId);
}

// same person cant upvote and downvote the same post, and cant vote twice in the same direction.
// 1000 bliver til 1k
// implement vote repository

public class VoteService : IVoteService {
    private readonly PostRepository _repository;

    public VoteService(PostRepository repository) {
        _repository = repository;
    }

    public void upvote(int postId, int authorId) {
        var post = _repository.GetObservation(postId);
        // check if author already upvoted
        _repository.upvote()
    }
}