using System.Net;
using System.Text.RegularExpressions;
using Bison.Razor.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class LoginTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        // xUnit makes a new instance per test, so each test gets a fresh
        // in-memory database, like in TimelineApiTests.
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
    public async Task AnonymousUserSeesRegisterAndLoginInNavigation()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/obs");

        Assert.Contains("Public Observations", html);
        Assert.Contains("/Identity/Account/Register", html);
        Assert.Contains("/Identity/Account/Login", html);
        Assert.DoesNotContain("Logout", html);
    }

    [Theory]
    [InlineData("edka@itu.dk", "123456")]
    [InlineData("debloh@itu.dk", "password")]
    public async Task SeededUserCanLogInWithTheirPassword(string email, string password)
    {
        // Don't follow redirects: a successful login answers with a 302.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await PostLoginAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith(".AspNetCore.Identity.Application="));
    }

    [Fact]
    public async Task LoginWithWrongPasswordFails()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await PostLoginAsync(client, "edka@itu.dk", "wrong-password");

        // A failed login re-renders the login page (200) instead of redirecting.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid login attempt", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoggedInUserSeesLogoutAndMyObservations()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await PostLoginAsync(client, "edka@itu.dk", "123456");

        var html = await client.GetStringAsync("/obs");

        Assert.Contains("Logout [edka@itu.dk]", html);
        Assert.Contains("href=\"/obs/Eduard\"", html);
        Assert.DoesNotContain("/Identity/Account/Register", html);
    }

    // The login form contains a hidden antiforgery token that must be sent
    // back with the POST, so we GET the page first and copy the token.
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
