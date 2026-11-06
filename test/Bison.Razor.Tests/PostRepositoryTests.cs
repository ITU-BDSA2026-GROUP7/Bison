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
    private readonly Taxon _taxon;

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
        _taxon = new Taxon { DwcTaxonId = "test-grey-heron", DanishVernacularName = "Fiskehejre" };
        _context.Authors.Add(_author);
        _context.Taxa.Add(_taxon);
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
        var id = _repository.CreateObservation(_author.Id, "Heron at DR Byen", null);

        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.Equal("Heron at DR Byen", stored.Text);
        Assert.Equal(_author.Id, stored.AuthorId);
    }

    [Fact]
    public void CreateObservationSetsTimestampToNow()
    {
        var before = DateTime.UtcNow;

        var id = _repository.CreateObservation(_author.Id, "Kingfisher by the canal", null);

        var after = DateTime.UtcNow;
        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.InRange(stored.TimeStamp, before, after);
    }

    [Fact]
    public void CreatedObservationShowsUpInPublicAndAuthorTimelines()
    {
        _repository.CreateObservation(_author.Id, "Grey heron on the pier", null);

        var publicList = _repository.GetObservations(0, 32);
        var authorList = _repository.GetObservationsFromAuthor(_author.Name, 0, 32);

        Assert.Contains(publicList, o => o.Text == "Grey heron on the pier");
        Assert.Contains(authorList, o => o.Text == "Grey heron on the pier");
    }

    [Fact]
    public void CreateObservationGivesEachObservationItsOwnId()
    {
        var first = _repository.CreateObservation(_author.Id, "First", null);
        var second = _repository.CreateObservation(_author.Id, "Second", null);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateObservationStoresTheChosenTaxon()
    {
        var id = _repository.CreateObservation(_author.Id, "Grey heron", _taxon.DwcTaxonId);

        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.Equal(_taxon.DwcTaxonId, stored.TaxonId);
    }

    // "Don't know" in the dropdown is sent as an empty string.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateObservationWithoutTaxonStoresNoTaxon(string? taxonId)
    {
        var id = _repository.CreateObservation(_author.Id, "Some bird", taxonId);

        var stored = _context.Observations.Single(o => o.Id == id);
        Assert.Null(stored.TaxonId);
    }

    [Fact]
    public void CreateObservationRejectsUnknownTaxon()
    {
        Assert.Throws<ArgumentException>(
            () => _repository.CreateObservation(_author.Id, "Mystery bird", "not-a-real-taxon"));
        Assert.Empty(_context.Observations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateObservationRejectsEmptyText(string text)
    {
        Assert.Throws<ArgumentException>(() => _repository.CreateObservation(_author.Id, text, null));
        Assert.Empty(_context.Observations);
    }

    [Fact]
    public void CreateObservationRejectsUnknownAuthor()
    {
        Assert.Throws<ArgumentException>(() => _repository.CreateObservation(9999, "Ghost sighting", null));
        Assert.Empty(_context.Observations);
    }
}
