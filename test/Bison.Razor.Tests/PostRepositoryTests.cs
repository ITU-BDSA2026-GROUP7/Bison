using Bison.Core.Domain;
using Bison.Infrastructure.Persistence;
using Bison.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Unit tests for PostRepository.CreateObservation. These talk to the
// repository directly (no web server), using a fresh in-memory SQLite
// database per test, so they are fast and independent of each other.
public class PostRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BisonDBContext _context;
    private readonly PostRepository _repository;
    private readonly Author _author;

    public PostRepositoryTests()
    {
        // An in-memory SQLite database lives only as long as its connection is open.
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

        _repository = new PostRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void CreateObservationStoresItInTheDatabase()
    {
        var id = _repository.CreateObservation(_author.Id, "Heron at DR Byen");

        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.Equal("Heron at DR Byen", stored.Text);
        Assert.Equal(_author.Id, stored.AuthorId);
    }

    [Fact]
    public void CreateObservationSetsTimestampToNow()
    {
        var before = DateTime.UtcNow;

        var id = _repository.CreateObservation(_author.Id, "Kingfisher by the canal");

        var after = DateTime.UtcNow;
        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.InRange(stored.TimeStamp, before, after);
    }

    [Fact]
    public void CreatedObservationShowsUpInPublicAndAuthorTimelines()
    {
        _repository.CreateObservation(_author.Id, "Grey heron on the pier");

        var publicList = _repository.GetObservations(0, 32);
        var authorList = _repository.GetObservationsFromAuthor(_author.Name, 0, 32);

        Assert.Contains(publicList, o => o.Text == "Grey heron on the pier");
        Assert.Contains(authorList, o => o.Text == "Grey heron on the pier");
    }

    [Fact]
    public void CreateObservationGivesEachObservationItsOwnId()
    {
        var first = _repository.CreateObservation(_author.Id, "First");
        var second = _repository.CreateObservation(_author.Id, "Second");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateObservationRejectsEmptyText(string text)
    {
        Assert.Throws<ArgumentException>(() => _repository.CreateObservation(_author.Id, text));
        Assert.Empty(_context.Observations);
    }

    [Fact]
    public void CreateObservationRejectsUnknownAuthor()
    {
        Assert.Throws<ArgumentException>(() => _repository.CreateObservation(9999, "Ghost sighting"));
        Assert.Empty(_context.Observations);
    }
}
