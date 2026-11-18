using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bison.Razor.E2E.Tests;

// Week 11: "Making comments and proposals" - authenticated users can comment and
// propose a taxon on the details page of an observation (/ob/{id}).
[Collection("Bison")]
public class CommentAndProposalE2ETests : E2ETestBase
{
    public CommentAndProposalE2ETests(BisonFixture fixture) : base(fixture) { }

    private string DetailsUrl => $"/ob/{SeededObservationId}";

    [Fact]
    public async Task AnonymousVisitorSeesNeitherCommentNorProposalForm()
    {
        await Page.GotoAsync(DetailsUrl);

        await Expect(Page.Locator("h3:has-text('Comments')")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[type='submit'][value='Comment']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("input[type='submit'][value='Propose']")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task LoggedInUserSeesCommentAndProposalForm()
    {
        await LogInAsync();

        await Page.GotoAsync(DetailsUrl);

        await Expect(Page.Locator("input[type='submit'][value='Comment']")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[type='submit'][value='Propose']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CommentShowsUpUnderTheObservationAndIsStoredForTheAuthor()
    {
        await LogInAsync();
        await Page.GotoAsync(DetailsUrl);
        var text = UniqueText("Lovely photo");

        await Page.Locator("input[name='CommentText']").FillAsync(text);
        await Page.Locator("input[type='submit'][value='Comment']").ClickAsync();

        var comment = ListItemWith(text);
        await Expect(comment).ToBeVisibleAsync();
        await Expect(comment).ToContainTextAsync("Eduard");

        // Comment and Proposal both have ObservationId, and Observation and Proposal both
        // have TaxonId. In EF's single table (Posts) the second owner of such a name can get
        // a prefixed column, so the checks below find columns by name pattern, not exact name.
        var row = Assert.Single(Fixture.Query(
            @"SELECT a.Name, p.Discriminator
              FROM Posts p JOIN AspNetUsers a ON a.Id = p.AuthorId
              WHERE p.Text = $text",
            ("$text", text)));
        Assert.Equal("Eduard", row[0]);
        Assert.Equal("Comment", row[1]);
        Assert.True(
            RowHasValue(text, "ObservationId", (long)SeededObservationId, out var dump),
            $"Comment is not linked to observation {SeededObservationId}. Row: {dump}");
    }

    [Fact]
    public async Task EmptyCommentShowsAnErrorAndStoresNothing()
    {
        await LogInAsync();
        await Page.GotoAsync(DetailsUrl);
        var before = Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Comment'");

        await Page.Locator("input[type='submit'][value='Comment']").ClickAsync();

        await Expect(Page.GetByText("Write a comment before sharing.")).ToBeVisibleAsync();
        Assert.Equal(before, Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Comment'"));
    }

    [Fact]
    public async Task ProposalShowsUpWithItsTaxonAndIsStoredForTheAuthor()
    {
        await LogInAsync(DebasmitaEmail, DebasmitaPassword);
        await Page.GotoAsync(DetailsUrl);
        var text = UniqueText("Yellow bill, so I think it is a grey heron");

        await Page.Locator("select[name='ProposedTaxonId']")
            .SelectOptionAsync(new SelectOptionValue { Label = GreyHeronName });
        await Page.Locator("input[name='ProposalText']").FillAsync(text);
        await Page.Locator("input[type='submit'][value='Propose']").ClickAsync();

        var proposal = ListItemWith(text);
        await Expect(proposal).ToBeVisibleAsync();
        await Expect(proposal).ToContainTextAsync("Debasmita");
        await Expect(proposal).ToContainTextAsync($"Taxon: {GreyHeronName}");

        var row = Assert.Single(Fixture.Query(
            @"SELECT a.Name, p.Discriminator
              FROM Posts p JOIN AspNetUsers a ON a.Id = p.AuthorId
              WHERE p.Text = $text",
            ("$text", text)));
        Assert.Equal("Debasmita", row[0]);
        Assert.Equal("Proposal", row[1]);

        Assert.True(
            RowHasValue(text, "ObservationId", (long)SeededObservationId, out var dump),
            $"Proposal is not linked to observation {SeededObservationId}. Row: {dump}");
        Assert.True(
            RowHasValue(text, "TaxonId", GreyHeronId, out dump),
            $"Proposal does not store the grey heron taxon. Row: {dump}");
    }

    // True if some column whose name contains `columnPart` holds `expected` in the row with this text.
    private bool RowHasValue(string text, string columnPart, object expected, out string dump)
    {
        var columns = Fixture.Query("SELECT name FROM pragma_table_info('Posts') ORDER BY cid")
            .Select(r => (string)r[0]!).ToList();
        var values = Assert.Single(Fixture.Query("SELECT * FROM Posts WHERE Text = $text", ("$text", text)));
        var cells = columns.Zip(values, (name, value) => (Name: name, Value: value)).ToList();
        dump = string.Join(", ", cells.Select(c => $"{c.Name}={c.Value ?? "NULL"}"));
        return cells.Any(c => c.Name.Contains(columnPart) && Equals(c.Value, expected));
    }

    [Fact]
    public async Task ProposalWithoutTaxonIsRejected()
    {
        await LogInAsync();
        await Page.GotoAsync(DetailsUrl);
        var text = UniqueText("No taxon chosen");
        var before = Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Proposal'");

        await Page.Locator("input[name='ProposalText']").FillAsync(text);
        await Page.Locator("input[type='submit'][value='Propose']").ClickAsync();

        await Expect(Page.GetByText("Pick a taxon from the list.")).ToBeVisibleAsync();
        Assert.Equal(before, Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Proposal'"));
    }

    // The full user journey: make an observation, open it, comment on it.
    [Fact]
    public async Task UserCanMakeAnObservationOpenItAndCommentOnIt()
    {
        await LogInAsync();
        var observation = UniqueText("Cormorant on the pier");
        var comment = UniqueText("Great spot");

        await ShareObservationAsync(observation);
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = observation }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/ob/\d+$"));
        await Expect(Page.Locator(".observationbox").First).ToContainTextAsync(observation);

        await Page.Locator("input[name='CommentText']").FillAsync(comment);
        await Page.Locator("input[type='submit'][value='Comment']").ClickAsync();

        await Expect(ListItemWith(comment)).ToBeVisibleAsync();
        await Expect(Page.Locator(".observationbox").First).ToContainTextAsync(observation);
    }
}
