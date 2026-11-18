using System.Net;
using System.Text.RegularExpressions;
using Bison.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Integration tests for the "about me" page: they start the whole web
// app in memory and send real HTTP requests, like LoginTests.
public class AboutMePageTests : IAsyncLifetime
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
    public async Task AnonymousUserDoesNotSeeAboutMeLink()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/obs");

        Assert.DoesNotContain("about me", html);
    }

    [Fact]
    public async Task AnonymousUserIsSentToLoginFromAboutMe()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/aboutme");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task LoggedInUserSeesAboutMeLinkBetweenTimelinesAndLogout()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await PostLoginAsync(client, "edka@itu.dk", "123456");

        var html = await client.GetStringAsync("/obs");

        var publicLink = html.IndexOf("Public Observations", StringComparison.Ordinal);
        var aboutMeLink = html.IndexOf(">about me</a>", StringComparison.Ordinal);
        var logout = html.IndexOf("Logout", StringComparison.Ordinal);
        Assert.True(publicLink >= 0 && aboutMeLink > publicLink && logout > aboutMeLink,
            "The about me link should sit between Public Observations and Logout");
    }

    [Fact]
    public async Task LoggedInUserSeesOwnPersonalData()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await PostLoginAsync(client, "edka@itu.dk", "123456");

        var response = await client.GetAsync("/aboutme");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Eduard", html);
        Assert.Contains("edka@itu.dk", html);
        Assert.DoesNotContain("debloh@itu.dk", html);
    }

    // Same helper as in LoginTests: copy the antiforgery token from the
    // login form and send it back with the POST.
    private static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client, string email, string password)
    {
        var loginPage = await client.GetStringAsync("/Identity/Account/Login");
        var token = Regex.Match(loginPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "Antiforgery token not found on login page");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        });

        return await client.PostAsync("/Identity/Account/Login", form);
    }
}
