using Bison.Core.Domain;
using Bison.Infrastructure.Persistence;
using Bison.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Unit tests for AuthorRepository.ForgetAuthor ("Forget me!").
public class ForgetMeRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BisonDBContext _context;
    private readonly AuthorRepository _authors;
    private readonly PostRepository _posts;
    private readonly Author _me;
    private readonly Author _other;
    private readonly Taxon _taxon;

    public ForgetMeRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BisonDBContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BisonDBContext(options);
        _context.Database.EnsureCreated();

        _me = new Author { Name = "Me", UserName = "me@itu.dk", Email = "me@itu.dk", PasswordHash = "not-a-real-hash" };
        _other = new Author { Name = "Someone else", UserName = "else@itu.dk", Email = "else@itu.dk" };
        _taxon = new Taxon { DwcTaxonId = "test-grey-heron", DanishVernacularName = "Fiskehejre" };
        _context.Authors.AddRange(_me, _other);
        _context.Taxa.Add(_taxon);
        _context.SaveChanges();

        _authors = new AuthorRepository(_context);
        _posts = new PostRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ForgetAuthor deletes with direct SQL, so forget what the context has cached.
    private List<string> Forget(int authorId, bool keepObservations)
    {
        var paths = _authors.ForgetAuthor(authorId, keepObservations);
        _context.ChangeTracker.Clear();
        return paths;
    }

    [Fact]
    public void ForgetDeletesTheUserRow()
    {
        Forget(_me.Id, keepObservations: true);

        Assert.False(_context.Authors.Any(a => a.Id == _me.Id));
        Assert.Null(_authors.GetUserInfo(_me.Id));
    }

    [Fact]
    public void ForgetDeletesLoginsAndClaims()
    {
        _context.UserLogins.Add(new IdentityUserLogin<int>
        {
            LoginProvider = "GitHub",
            ProviderKey = "12345",
            ProviderDisplayName = "GitHub",
            UserId = _me.Id
        });
        _context.UserClaims.Add(new IdentityUserClaim<int>
        {
            UserId = _me.Id,
            ClaimType = "test",
            ClaimValue = "x"
        });
        _context.SaveChanges();

        Forget(_me.Id, keepObservations: true);

        Assert.Empty(_context.UserLogins.Where(l => l.UserId == _me.Id));
        Assert.Empty(_context.UserClaims.Where(c => c.UserId == _me.Id));
    }

    [Fact]
    public void ForgetDeletesMyCommentsAndProposalsOnOthersObservations()
    {
        var othersObservation = _posts.CreateObservation(_other.Id, "Bird at the lake", null);
        _posts.CreateComment(_me.Id, othersObservation, "Nice!");
        _posts.CreateProposal(_me.Id, othersObservation, _taxon.DwcTaxonId, "Looks like a heron");

        Forget(_me.Id, keepObservations: true);

        Assert.Empty(_posts.GetComments(othersObservation, 0, 32));
        Assert.Empty(_posts.GetProposals(othersObservation, 0, 32));
    }

    [Fact]
    public void KeepingObservationsShowsThemAsDeletedUser()
    {
        var id = _posts.CreateObservation(_me.Id, "My heron", _taxon.DwcTaxonId);

        Forget(_me.Id, keepObservations: true);

        var observation = _posts.GetObservations(0, 32).Single(o => o.Id == id);
        Assert.Equal("My heron", observation.Text);
        Assert.Equal("deleted user", observation.AuthorName);
    }

    [Fact]
    public void KeepingObservationsLeavesOthersCommentsInPlace()
    {
        var id = _posts.CreateObservation(_me.Id, "My heron", null);
        _posts.CreateComment(_other.Id, id, "Great photo");

        Forget(_me.Id, keepObservations: true);

        Assert.Single(_posts.GetComments(id, 0, 32));
    }

    [Fact]
    public void NotKeepingObservationsDeletesThemAndTheirComments()
    {
        var id = _posts.CreateObservation(_me.Id, "My heron", null, "/uploads/observations/abc.jpg");
        _posts.CreateComment(_other.Id, id, "Great photo");
        _posts.CreateProposal(_other.Id, id, _taxon.DwcTaxonId, "Heron?");

        var paths = Forget(_me.Id, keepObservations: false);

        Assert.False(_context.Observations.Any(o => o.Id == id));
        Assert.Empty(_context.Comments.Where(c => c.ObservationId == id));
        Assert.Empty(_context.Proposals.Where(p => p.ObservationId == id));
        Assert.Equal(new[] { "/uploads/observations/abc.jpg" }, paths);
    }

    [Fact]
    public void ForgetLeavesOtherUsersDataAlone()
    {
        var othersObservation = _posts.CreateObservation(_other.Id, "Their bird", null);
        _posts.CreateComment(_other.Id, othersObservation, "Their comment");

        Forget(_me.Id, keepObservations: false);

        Assert.True(_context.Authors.Any(a => a.Id == _other.Id));
        Assert.True(_context.Observations.Any(o => o.Id == othersObservation));
        Assert.Single(_posts.GetComments(othersObservation, 0, 32));
    }

    [Fact]
    public void ForgettingSeveralUsersUsesOnePlaceholder()
    {
        _posts.CreateObservation(_me.Id, "Mine", null);
        _posts.CreateObservation(_other.Id, "Theirs", null);

        Forget(_me.Id, keepObservations: true);
        Forget(_other.Id, keepObservations: true);

        Assert.Equal(1, _context.Authors.Count(a => a.UserName == "deleted-user"));
    }

    [Fact]
    public void PlaceholderCannotLogInOrBeForgotten()
    {
        _posts.CreateObservation(_me.Id, "Mine", null);
        Forget(_me.Id, keepObservations: true);

        var placeholder = _context.Authors.Single(a => a.UserName == "deleted-user");

        Assert.Null(placeholder.PasswordHash);
        Assert.Equal(DateTimeOffset.MaxValue, placeholder.LockoutEnd);
        Assert.Throws<InvalidOperationException>(() => _authors.ForgetAuthor(placeholder.Id, true));
    }

    [Fact]
    public void ForgetRejectsUnknownAuthor()
    {
        Assert.Throws<ArgumentException>(() => _authors.ForgetAuthor(9999, true));
    }
}