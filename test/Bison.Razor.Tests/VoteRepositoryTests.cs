using Bison.Core.Domain;
using Bison.Infrastructure.Persistence;
using Bison.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class VoteRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BisonDBContext _context;
    private readonly VoteRepository _repository;
    private readonly Author _author;
    private readonly Observation _observation;

    public VoteRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BisonDBContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BisonDBContext(options);
        _context.Database.EnsureCreated();

        _author = new Author { Name = "Test author", UserName = "test@itu.dk" };
        _context.Authors.Add(_author);
        _context.SaveChanges();

        _observation = new Observation
        {
            AuthorId = _author.Id,
            Text = "A bird",
            TimeStamp = DateTime.UtcNow
        };
        _context.Observations.Add(_observation);
        _context.SaveChanges();

        _repository = new VoteRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public void VotePersistsVoteAndIncrementsMatchingCounter(
        bool direction,
        int expectedUpVotes,
        int expectedDownVotes)
    {
        _repository.Vote(_observation.Id, _author.Id, direction);

        _context.ChangeTracker.Clear();

        var storedVote = _context.Votes.Single();
        var storedPost = _context.Posts.Single(p => p.Id == _observation.Id);

        Assert.Equal(_observation.Id, storedVote.PostId);
        Assert.Equal(_author.Id, storedVote.AuthorId);
        Assert.Equal(direction, storedVote.Direction);
        Assert.Equal(expectedUpVotes, storedPost.UpVotes);
        Assert.Equal(expectedDownVotes, storedPost.DownVotes);
    }

    [Fact]
    public void VoteRejectsDuplicateVoteInSameDirection()
    {
        _repository.Vote(_observation.Id, _author.Id, true);

        Assert.Throws<ArgumentException>(
            () => _repository.Vote(_observation.Id, _author.Id, true));

        Assert.Single(_context.Votes);
    }

    [Fact]
    public void HasVotedPostMatchesPostAuthorAndDirection()
    {
        Assert.False(_repository.HasVotedPost(_observation.Id, _author.Id, true));

        _repository.Vote(_observation.Id, _author.Id, true);

        Assert.True(_repository.HasVotedPost(_observation.Id, _author.Id, true));
        Assert.False(_repository.HasVotedPost(_observation.Id, _author.Id, false));
    }

    [Fact]
    public void VoteRejectsOppositeDirectionAfterExistingVote()
    {
        _repository.Vote(_observation.Id, _author.Id, true);

        Assert.Throws<ArgumentException>(
            () => _repository.Vote(_observation.Id, _author.Id, false));

        _context.ChangeTracker.Clear();
        var storedPost = _context.Posts.Single(p => p.Id == _observation.Id);
        Assert.Equal(1, storedPost.UpVotes);
        Assert.Equal(0, storedPost.DownVotes);
        Assert.Single(_context.Votes);
    }
}
