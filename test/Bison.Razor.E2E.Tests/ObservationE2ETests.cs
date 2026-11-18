using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bison.Razor.E2E.Tests;

// Week 11: "Making Observations" - only authenticated users get the input field,
// it sits under the <h2>, and a shared observation lands on the public list and
// on the author's own list (and in the database, for the right author).
[Collection("Bison")]
public class ObservationE2ETests : E2ETestBase
{
    public ObservationE2ETests(BisonFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AnonymousVisitorDoesNotSeeTheObservationBox()
    {
        await Page.GotoAsync("/obs");

        await Expect(Page.Locator("h2")).ToContainTextAsync("Public Observations");
        await Expect(Page.Locator(".observationbox")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ObservationBoxAppearsOnlyAfterSuccessfulLogin()
    {
        await Page.GotoAsync("/obs");
        await Expect(Page.Locator(".observationbox")).ToHaveCountAsync(0);

        await LogInAsync();

        await Expect(Page.Locator(".observationbox")).ToBeVisibleAsync();
        await Expect(Page.Locator(".observationbox h3")).ToHaveTextAsync($"What did you see, {EduardEmail}?");
    }

    [Fact]
    public async Task FailedLoginDoesNotShowTheObservationBox()
    {
        await Page.GotoAsync("/Identity/Account/Login");
        await Page.Locator("#Input_Email").FillAsync(EduardEmail);
        await Page.Locator("#Input_Password").FillAsync("wrong-password");
        await Page.Locator("#login-submit").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Identity/Account/Login"));
        await Page.GotoAsync("/obs");
        await Expect(Page.Locator(".observationbox")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ObservationBoxIsDirectlyUnderThePublicObservationsHeading()
    {
        await LogInAsync();

        // Sibling selector: the box must come after the <h2> inside the same container.
        await Expect(Page.Locator("h2:has-text('Public Observations') ~ .observationbox")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ObservationBoxIsUnderMyObservationsHeadingOnOwnTimeline()
    {
        await LogInAsync();

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "My Observations" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/obs/Eduard$"));
        await Expect(Page.Locator("h2:has-text('My Observations') ~ .observationbox")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task NoObservationBoxOnSomeoneElsesTimeline()
    {
        await LogInAsync();

        await Page.GotoAsync("/obs/Debasmita");

        await Expect(Page.Locator(".observationbox")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task SharedObservationAppearsOnPublicListAndOnAuthorsOwnList()
    {
        await LogInAsync();
        var text = UniqueText("Heron at DR Byen");

        await ShareObservationAsync(text);

        // Public list (the app redirects back to /obs after posting).
        await Expect(ListItemWith(text)).ToBeVisibleAsync();
        await Expect(ListItemWith(text)).ToContainTextAsync("Eduard");

        // The author's own list.
        await Page.GotoAsync("/obs/Eduard");
        await Expect(ListItemWith(text)).ToBeVisibleAsync();

        // Somebody else's list must not contain it.
        await Page.GotoAsync("/obs/Debasmita");
        await Expect(ListItemWith(text)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ObservationCanBeSharedFromOwnTimelineToo()
    {
        await LogInAsync();
        await Page.GotoAsync("/obs/Eduard");
        var text = UniqueText("Kingfisher");

        await ShareObservationAsync(text);

        await Expect(ListItemWith(text)).ToBeVisibleAsync();
        await Page.GotoAsync("/obs");
        await Expect(ListItemWith(text)).ToBeVisibleAsync();
    }

    // The requirement from the handout: the observation is stored for the respective author.
    [Theory]
    [InlineData(EduardEmail, EduardPassword, "Eduard")]
    [InlineData(DebasmitaEmail, DebasmitaPassword, "Debasmita")]
    public async Task ObservationIsStoredInDatabaseForTheRespectiveAuthor(string email, string password, string name)
    {
        await LogInAsync(email, password);
        var text = UniqueText($"Stored for {name}");

        await ShareObservationAsync(text);
        await Expect(ListItemWith(text)).ToBeVisibleAsync(); // post finished

        var rows = Fixture.Query(
            @"SELECT a.Name, a.Email, p.Discriminator
              FROM Posts p JOIN AspNetUsers a ON a.Id = p.AuthorId
              WHERE p.Text = $text",
            ("$text", text));

        var row = Assert.Single(rows);
        Assert.Equal(name, row[0]);
        Assert.Equal(email, row[1]);
        Assert.Equal("Observation", row[2]);
    }

    [Fact]
    public async Task ObservationWithTaxonIsStoredWithThatTaxonAndFoundByTheFilter()
    {
        await LogInAsync();
        var text = UniqueText("Grey heron by the lake");

        await ShareObservationAsync(text, GreyHeronName);
        await Expect(ListItemWith(text)).ToBeVisibleAsync();

        var rows = Fixture.Query("SELECT TaxonId FROM Posts WHERE Text = $text", ("$text", text));
        Assert.Equal(GreyHeronId, Assert.Single(rows)[0]);

        await Page.GotoAsync($"/obs?taxonId={Uri.EscapeDataString(GreyHeronId)}");
        await Expect(ListItemWith(text)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ObservationWithoutTaxonIsStoredWithoutTaxon()
    {
        await LogInAsync();
        var text = UniqueText("Unknown bird");

        await ShareObservationAsync(text); // "Don't know" stays selected
        await Expect(ListItemWith(text)).ToBeVisibleAsync();

        var rows = Fixture.Query("SELECT TaxonId FROM Posts WHERE Text = $text", ("$text", text));
        Assert.Null(Assert.Single(rows)[0]);
    }

    [Fact]
    public async Task EmptyObservationShowsAnErrorAndStoresNothing()
    {
        await LogInAsync();
        var before = Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Observation'");

        await Page.Locator(".observationbox input[type='submit'][value='Share']").ClickAsync();

        await Expect(Page.GetByText("Write what you saw before sharing.")).ToBeVisibleAsync();
        Assert.Equal(before, Fixture.Count("SELECT COUNT(*) FROM Posts WHERE Discriminator = 'Observation'"));
    }

    [Fact]
    public async Task ObservationBoxDisappearsAgainAfterLogout()
    {
        await LogInAsync();
        await Expect(Page.Locator(".observationbox")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = $"Logout [{EduardEmail}]" }).ClickAsync();

        await Expect(Page.Locator(".observationbox")).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Login" })).ToBeVisibleAsync();
    }
}
