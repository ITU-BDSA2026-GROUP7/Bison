using System.Net;
using System.Text.RegularExpressions;
using Bison.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// End-to-end tests: start the whole web app in memory, log in as a
// seeded user and use the "What did you see?" form like a browser would.
public class MakeObservationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BisonDBContext>>();
                    services.AddDbContext<BisonDBContext>(options => options.UseSqlite(_connection));
                });
            });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AnonymousUserDoesNotSeeTheObservationBox()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/obs");

        Assert.DoesNotContain("observationbox", html);
    }

    [Fact]
    public async Task LoggedInUserSeesTheObservationBoxOnPublicAndOwnTimeline()
    {
        using var client = CreateClient();
        await LogInAsync(client, "edka@itu.dk", "123456");

        var publicHtml = await client.GetStringAsync("/obs");
        var ownHtml = await client.GetStringAsync("/obs/Eduard");

        Assert.Contains("observationbox", publicHtml);
        Assert.Contains("What did you see, edka@itu.dk?", publicHtml);
        Assert.Contains("My Observations </h2>", ownHtml);
        Assert.Contains("observationbox", ownHtml);
    }

    [Fact]
    public async Task LoggedInUserDoesNotSeeTheBoxOnSomeoneElsesTimeline()
    {
        using var client = CreateClient();
        await LogInAsync(client, "edka@itu.dk", "123456");

        var html = await client.GetStringAsync("/obs/SomeoneElse");

        Assert.DoesNotContain("observationbox", html);
    }

    [Fact]
    public async Task SharedObservationAppearsOnPublicAndAuthorTimeline()
    {
        using var client = CreateClient();
        await LogInAsync(client, "edka@itu.dk", "123456");
        var text = $"Heron at DR Byen {Guid.NewGuid():N}";

        var response = await PostObservationAsync(client, "/obs", text);

        // Successful post redirects back (Post/Redirect/Get).
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(text, await client.GetStringAsync("/obs"));
        Assert.Contains(text, await client.GetStringAsync("/obs/Eduard"));
    }

    [Fact]
    public async Task ObservationCanAlsoBeSharedFromOwnTimeline()
    {
        using var client = CreateClient();
        await LogInAsync(client, "edka@itu.dk", "123456");
        var text = $"Kingfisher {Guid.NewGuid():N}";

        var response = await PostObservationAsync(client, "/obs/Eduard", text);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(text, await client.GetStringAsync("/obs/Eduard"));
        Assert.Contains(text, await client.GetStringAsync("/obs"));
    }

    [Fact]
    public async Task EmptyObservationIsNotStored()
    {
        using var client = CreateClient();
        await LogInAsync(client, "edka@itu.dk", "123456");
        var countBefore = CountObservations();

        var response = await PostObservationAsync(client, "/obs", "");

        // The page is shown again with an error instead of redirecting.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(countBefore, CountObservations());
    }

    [Fact]
    public async Task AnonymousUserCannotPostAnObservation()
    {
        using var client = CreateClient();
        var countBefore = CountObservations();

        // An anonymous user has no form, so no antiforgery token either.
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Text"] = "Sneaky anonymous observation"
        });
        var response = await client.PostAsync("/obs", form);

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(countBefore, CountObservations());
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private int CountObservations()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<BisonDBContext>().Observations.Count();
    }

    // Every POST form contains a hidden antiforgery token that must be sent back.
    private static string GetAntiforgeryToken(string html)
    {
        var token = Regex.Match(html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "Antiforgery token not found on page");
        return token;
    }

    private static async Task LogInAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetStringAsync("/Identity/Account/Login");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = GetAntiforgeryToken(loginPage)
        });
        var response = await client.PostAsync("/Identity/Account/Login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostObservationAsync(
        HttpClient client, string url, string text)
    {
        var page = await client.GetStringAsync(url);
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Text"] = text,
            ["__RequestVerificationToken"] = GetAntiforgeryToken(page)
        });
        return await client.PostAsync(url, form);
    }
}
