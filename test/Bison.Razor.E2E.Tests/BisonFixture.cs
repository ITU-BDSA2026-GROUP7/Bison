using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;

namespace Bison.Razor.E2E.Tests;

// Starts the real Bison web app (Kestrel, a fresh SQLite file) once for all
// E2E tests and shares one headless Chromium between them. Playwright needs a
// real HTTP server, so WebApplicationFactory (in-memory TestServer) is not enough.
public class BisonFixture : IAsyncLifetime
{
    private Process? _app;
    private readonly StringBuilder _log = new();
    private IPlaywright _playwright = null!;

    public string BaseUrl { get; private set; } = "";
    public string DbPath { get; } = Path.Combine(Path.GetTempPath(), $"bison-e2e-{Guid.NewGuid():N}.db");
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var repoRoot = FindRepoRoot();
        var razorDir = Path.Combine(repoRoot, "src", "Bison.Razor");

#if DEBUG
        const string config = "Debug";
#else
        const string config = "Release";
#endif

        var psi = new ProcessStartInfo("dotnet")
        {
            // Content root must be the Razor project, otherwise wwwroot (CSS, images) is not found.
            WorkingDirectory = razorDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in new[] { "run", "--project", razorDir, "-c", config, "--no-launch-profile" })
        {
            psi.ArgumentList.Add(arg);
        }
        psi.Environment["ASPNETCORE_URLS"] = BaseUrl;
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["BISON_EF_DBPATH"] = DbPath;
        // Program.cs reads these with "!", and the GitHub handler refuses to start
        // without them. The E2E tests never use GitHub login, so dummies are fine.
        psi.Environment["authentication__github__clientId"] = "e2e-dummy";
        psi.Environment["authentication__github__clientSecret"] = "e2e-dummy";

        _app = Process.Start(psi) ?? throw new InvalidOperationException("Could not start dotnet.");
        _app.OutputDataReceived += (_, e) => Append(e.Data);
        _app.ErrorDataReceived += (_, e) => Append(e.Data);
        _app.BeginOutputReadLine();
        _app.BeginErrorReadLine();

        await WaitUntilReadyAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        _playwright?.Dispose();

        if (_app is { HasExited: false })
        {
            _app.Kill(entireProcessTree: true);
            await _app.WaitForExitAsync();
        }
        _app?.Dispose();

        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { DbPath, DbPath + "-wal", DbPath + "-shm", DbPath + "-journal" })
        {
            try { File.Delete(file); } catch (IOException) { /* best effort */ }
        }
    }

    // ---- database access, used to check what the app really stored ----

    public List<object?[]> Query(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        var rows = new List<object?[]>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < row.Length; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }

    public long Count(string sql, params (string Name, object? Value)[] parameters) =>
        Convert.ToInt64(Query(sql, parameters)[0][0]);

    public void Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        // No pooling, so no connection (and no file lock) outlives a query.
        var connection = new SqliteConnection($"Data Source={DbPath};Pooling=False");
        connection.Open();
        return connection;
    }

    // ---- startup helpers ----

    private void Append(string? line)
    {
        if (line is null) return;
        lock (_log)
        {
            if (_log.Length < 20_000) _log.AppendLine(line);
        }
    }

    private async Task WaitUntilReadyAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(180); // first run builds + seeds ~100 KB of data

        while (DateTime.UtcNow < deadline)
        {
            if (_app!.HasExited)
            {
                throw new InvalidOperationException($"Bison exited during startup:\n{_log}");
            }

            try
            {
                var response = await http.GetAsync($"{BaseUrl}/obs");
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // not listening yet
            }
            await Task.Delay(500);
        }

        throw new TimeoutException($"Bison did not start within 180 s.\n{_log}");
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Bison.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find Bison.sln above the test output folder.");
    }
}

[CollectionDefinition("Bison")]
public class BisonCollection : ICollectionFixture<BisonFixture>
{
}

// Base class: one fresh browser context (= clean cookies, so logged out) per test.
public abstract class E2ETestBase : IAsyncLifetime
{
    protected const string EduardEmail = "edka@itu.dk";
    protected const string EduardPassword = "123456";
    protected const string DebasmitaEmail = "debloh@itu.dk";
    protected const string DebasmitaPassword = "password";

    // Taxon ids/names from the seed data in DbInitializer.
    protected const string GreyHeronId = "MSTSNM:Arter:c28811f4-f785-ea11-aa77-501ac539d1ea";
    protected const string GreyHeronName = "Fiskehejre";

    // A seeded observation, so it never has a picture and always exists.
    protected const int SeededObservationId = 1;

    protected readonly BisonFixture Fixture;
    protected IBrowserContext Context = null!;
    protected IPage Page = null!;

    protected E2ETestBase(BisonFixture fixture) => Fixture = fixture;

    public async Task InitializeAsync()
    {
        Context = await Fixture.Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = Fixture.BaseUrl });
        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync() => await Context.CloseAsync();

    protected static string UniqueText(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    protected async Task LogInAsync(string email = EduardEmail, string password = EduardPassword)
    {
        await Page.GotoAsync("/Identity/Account/Login");
        await Page.Locator("#Input_Email").FillAsync(email);
        await Page.Locator("#Input_Password").FillAsync(password);
        await Page.Locator("#login-submit").ClickAsync();
        // Identity sends the user to "/", which redirects to /obs.
        await Page.WaitForURLAsync("**/obs");
    }

    // Uses the observation box exactly like a user: type, (optionally) pick a taxon, press Share.
    protected async Task ShareObservationAsync(string text, string? taxonName = null)
    {
        var box = Page.Locator(".observationbox");
        if (taxonName is not null)
        {
            await box.Locator("select[name='ObservedTaxonId']")
                .SelectOptionAsync(new SelectOptionValue { Label = taxonName });
        }
        await box.Locator("input[name='Text']").FillAsync(text);
        await box.Locator("input[type='submit'][value='Share']").ClickAsync();
    }

    protected ILocator ListItemWith(string text) =>
        Page.Locator("ul.observations li").Filter(new LocatorFilterOptions { HasText = text });

    // Runs axe-core on the current page against WCAG 2.x A + AA and fails with a readable report.
    protected async Task AssertNoAccessibilityViolationsAsync()
    {
        var results = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions
            {
                Type = "tag",
                Values = new List<string> { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" }
            }
        });

        var report = string.Join("\n", results.Violations.Select(v =>
            $"- {v.Id} ({v.Impact}): {v.Help}\n" +
            string.Join("\n", v.Nodes.Select(n => $"    {n.Html}"))));

        Assert.True(results.Violations.Length == 0,
            $"axe found {results.Violations.Length} violation(s) on {Page.Url}:\n{report}");
    }
}
