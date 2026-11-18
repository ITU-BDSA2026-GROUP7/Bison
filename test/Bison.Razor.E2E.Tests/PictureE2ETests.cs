using static Microsoft.Playwright.Assertions;

namespace Bison.Razor.E2E.Tests;

// Week 11: "Pictures" - observations may have an optional picture; observations
// that existed before the feature have none.
//
// The observation box has no upload control (the handout only asks for the
// optional picture), so the picture is attached by setting PicturePath in the
// database, exactly what an upload would store. If you add an upload field,
// replace Fixture.Execute(...) below with Page.SetInputFilesAsync(...).
[Collection("Bison")]
public class PictureE2ETests : E2ETestBase
{
    private const string PicturePath = "/images/observations/heron.jpg";

    public PictureE2ETests(BisonFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ObservationsFromBeforeTheFeatureHaveNoPicture()
    {
        Assert.Null(Assert.Single(Fixture.Query(
            "SELECT PicturePath FROM Posts WHERE Id = $id", ("$id", SeededObservationId)))[0]);

        await Page.GotoAsync($"/ob/{SeededObservationId}");

        await Expect(Page.Locator("img.observation-picture")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task NewObservationWithoutPictureShowsNoImage()
    {
        await LogInAsync();
        var text = UniqueText("No picture today");

        await ShareObservationAsync(text);

        await Expect(ListItemWith(text)).ToBeVisibleAsync();
        await Expect(ListItemWith(text).Locator("img.observation-picture")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ObservationWithPictureShowsItOnPublicListAndOnDetailsPage()
    {
        await LogInAsync();
        var text = UniqueText("Heron with picture");
        await ShareObservationAsync(text);
        await Expect(ListItemWith(text)).ToBeVisibleAsync();

        Fixture.Execute("UPDATE Posts SET PicturePath = $path WHERE Text = $text",
            ("$path", PicturePath), ("$text", text));

        // Public list
        await Page.GotoAsync("/obs");
        var listPicture = ListItemWith(text).Locator("img.observation-picture");
        await Expect(listPicture).ToBeVisibleAsync();
        await Expect(listPicture).ToHaveAttributeAsync("alt", "Picture of the animal in the observation by Eduard");
        Assert.True(await listPicture.EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0"),
            "The picture file was not served (broken image).");

        // Details page
        await Page.GetByRole(Microsoft.Playwright.AriaRole.Link, new() { Name = text }).ClickAsync();
        var detailPicture = Page.Locator("img.observation-picture.detail");
        await Expect(detailPicture).ToBeVisibleAsync();
        Assert.True(await detailPicture.EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0"));
    }
}
