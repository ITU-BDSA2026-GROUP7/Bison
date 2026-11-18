using Bison.Core.Domain;
using Bison.Infrastructure.Persistence;
using Bison.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Unit tests for AuthorRepository.GetUserInfo, the data behind the
// "about me" page. Same setup as PostRepositoryTests: a fresh
// in-memory SQLite database per test.
public class AuthorRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BisonDBContext _context;
    private readonly AuthorRepository _repository;
    private readonly Author _me;
    private readonly Author _someoneElse;
    private readonly Taxon _taxon;

    public AuthorRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BisonDBContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BisonDBContext(options);
        _context.Database.EnsureCreated();

        _me = new Author
        {
            Name = "Me",
            UserName = "me@itu.dk",
            Email = "me@itu.dk",
            EmailConfirmed = true,
            PasswordHash = "not-a-real-hash"
        };
        _someoneElse = new Author { Name = "Someone else", UserName = "else@itu.dk" };
        _taxon = new Taxon { DwcTaxonId = "test-grey-heron", DanishVernacularName = "Fiskehejre" };
        _context.Authors.AddRange(_me, _someoneElse);
        _context.Taxa.Add(_taxon);
        _context.SaveChanges();

        _repository = new AuthorRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void GetUserInfoReturnsPersonalData()
    {
        var info = _repository.GetUserInfo(_me.Id)!;

        Assert.Equal("Me", info.Name);
        Assert.Equal("me@itu.dk", info.UserName);
        Assert.Equal("me@itu.dk", info.Email);
        Assert.True(info.EmailConfirmed);
        Assert.True(info.HasPassword);
    }

    [Fact]
    public void GetUserInfoReturnsNullForUnknownAuthor()
    {
        Assert.Null(_repository.GetUserInfo(9999));
    }

    [Fact]
    public void GetUserInfoListsOwnObservationsCommentsAndProposals()
    {
        var mine = AddObservation(_me, "My heron");
        AddComment(_me, mine, "My comment");
        AddProposal(_me, mine, "My proposal");

        var info = _repository.GetUserInfo(_me.Id)!;

        Assert.Equal("My heron", Assert.Single(info.Observations).Text);
        Assert.Equal("My comment", Assert.Single(info.Comments).Text);
        var proposal = Assert.Single(info.Proposals);
        Assert.Equal("My proposal", proposal.Text);
        Assert.Equal("Fiskehejre", proposal.TaxonName);
    }

    [Fact]
    public void GetUserInfoLeavesOutOtherUsersPosts()
    {
        var theirs = AddObservation(_someoneElse, "Their heron");
        AddComment(_someoneElse, theirs, "Their comment");
        AddProposal(_someoneElse, theirs, "Their proposal");

        var info = _repository.GetUserInfo(_me.Id)!;

        Assert.Empty(info.Observations);
        Assert.Empty(info.Comments);
        Assert.Empty(info.Proposals);
    }

    [Fact]
    public void GetUserInfoIncludesMyCommentsOnOtherUsersObservations()
    {
        var theirs = AddObservation(_someoneElse, "Their heron");
        AddComment(_me, theirs, "Nice one");

        var info = _repository.GetUserInfo(_me.Id)!;

        var comment = Assert.Single(info.Comments);
        Assert.Equal(theirs.Id, comment.ObservationId);
    }

    [Fact]
    public void GetUserInfoListsExternalLogins()
    {
        _context.UserLogins.Add(new IdentityUserLogin<int>
        {
            LoginProvider = "GitHub",
            ProviderKey = "12345",
            ProviderDisplayName = "GitHub",
            UserId = _me.Id
        });
        _context.SaveChanges();

        var info = _repository.GetUserInfo(_me.Id)!;

        Assert.Equal("GitHub", Assert.Single(info.ExternalLogins));
    }

    private Observation AddObservation(Author author, string text)
    {
        var observation = new Observation { AuthorId = author.Id, Text = text, TimeStamp = DateTime.UtcNow };
        _context.Observations.Add(observation);
        _context.SaveChanges();
        return observation;
    }

    private void AddComment(Author author, Observation observation, string text)
    {
        _context.Comments.Add(new Comment
        {
            AuthorId = author.Id, ObservationId = observation.Id, Text = text, TimeStamp = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    private void AddProposal(Author author, Observation observation, string text)
    {
        _context.Proposals.Add(new Proposal
        {
            AuthorId = author.Id, ObservationId = observation.Id, TaxonId = _taxon.DwcTaxonId,
            Text = text, TimeStamp = DateTime.UtcNow
        });
        _context.SaveChanges();
    }
}
