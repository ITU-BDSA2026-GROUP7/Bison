using Bison.Infrastructure.Persistence;
using Bison.Core.Domain;

namespace Bison.Infrastructure.Repositories;

public class VoteRepository : IVoteRepository
{   
    private readonly BisonDBContext _context;
    public VoteRepository(BisonDBContext context)
    {
        _context = context;
    }
    public void Vote(int postId, int authorId, bool direction)
    {
        if (HasVotedPost(postId, authorId, direction))
        {
            throw new ArgumentException(
                $"Author {authorId} has already cast this vote on post {postId}.",
                nameof(direction));
        }

        var post = _context.Posts.SingleOrDefault(p => p.Id == postId)
            ?? throw new ArgumentException($"No post with id {postId} exists.", nameof(postId));

        var author = _context.Authors.SingleOrDefault(a => a.Id == authorId)
            ?? throw new ArgumentException($"No author with id {authorId} exists.", nameof(authorId));

        if (direction)
        {
            post.UpVotes++;
        }
        else
        {
            post.DownVotes++;
        }

        var vote = new Vote
        {   
            PostId = postId,
            AuthorId = authorId,
            TimeStamp = DateTime.UtcNow,
            Direction = direction

        };

        _context.Votes.Add(vote);

        _context.SaveChanges();
    }

    public bool HasVotedPost(int postId, int authorId, bool direction)
    {
        return _context.Votes.Any(v =>
            v.PostId == postId &&
            v.AuthorId == authorId &&
            v.Direction == direction);
    }

}