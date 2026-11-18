namespace Bison.Razor.E2E.Tests;

// axe-core scans of the pages touched by the week 11 features, as anonymous
// visitor and as logged-in user (the logged-in pages contain the new forms).
[Collection("Bison")]
public class AccessibilityE2ETests : E2ETestBase
{
    public AccessibilityE2ETests(BisonFixture fixture) : base(fixture) { }

    [Fact]
    public async Task PublicObservationsAsAnonymousVisitorHasNoViolations()
    {
        await Page.GotoAsync("/obs");
        await AssertNoAccessibilityViolationsAsync();
    }

    [Fact]
    public async Task LoginPageHasNoViolations()
    {
        await Page.GotoAsync("/Identity/Account/Login");
        await AssertNoAccessibilityViolationsAsync();
    }

    [Fact]
    public async Task PublicObservationsWithObservationBoxHasNoViolations()
    {
        await LogInAsync();
        await AssertNoAccessibilityViolationsAsync();
    }

    [Fact]
    public async Task MyObservationsWithObservationBoxHasNoViolations()
    {
        await LogInAsync();
        await Page.GotoAsync("/obs/Eduard");
        await AssertNoAccessibilityViolationsAsync();
    }

    [Fact]
    public async Task ObservationDetailsAsAnonymousVisitorHasNoViolations()
    {
        await Page.GotoAsync($"/ob/{SeededObservationId}");
        await AssertNoAccessibilityViolationsAsync();
    }

    [Fact]
    public async Task ObservationDetailsWithCommentAndProposalFormsHasNoViolations()
    {
        await LogInAsync();
        await Page.GotoAsync($"/ob/{SeededObservationId}");
        await AssertNoAccessibilityViolationsAsync();
    }
}
