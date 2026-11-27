using Bison.Core.DTO;

namespace Bison.Infrastructure.Repositories;

public interface IVoteRepository
{
    public void Vote(int postId, int authorId, bool direction);
    public void RemoveVote(int postId, int authorId, bool direction);
    public bool HasVotedPost(int postId, int authorId, bool direction);
}