using Bison.Razor.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class TimelineApiTests
{
    [Fact]
    public async Task PublicTimelineDisplaysAnObservationFromTheDatabase()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BisonDBContext>>();
                    services.AddDbContext<BisonDBContext>(options => options.UseSqlite(connection));
                });
            });

        using var client = factory.CreateClient();
        var authorName = $"Test author {Guid.NewGuid():N}";
        var observationText = $"Test observation {Guid.NewGuid():N}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BisonDBContext>();
            dbContext.Observations.Add(new Observation
            {
                Text = observationText,
                TimeStamp = DateTime.UtcNow,
                Author = new Author { Name = authorName }
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/obs");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(authorName, html);
        Assert.Contains(observationText, html);
    }

    [Fact]
    public async Task PrivateTimelineDisplaysOnlyObservationsFromTheRequestedAuthor()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BisonDBContext>>();
                    services.AddDbContext<BisonDBContext>(options => options.UseSqlite(connection));
                });
            });

        using var client = factory.CreateClient();
        var authorName = $"Test author {Guid.NewGuid():N}";
        var observationText = $"Private timeline observation {Guid.NewGuid():N}";
        var otherObservationText = $"Other author's observation {Guid.NewGuid():N}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BisonDBContext>();
            dbContext.Observations.AddRange(
                new Observation
                {
                    Text = observationText,
                    TimeStamp = DateTime.UtcNow,
                    Author = new Author { Name = authorName }
                },
                new Observation
                {
                    Text = otherObservationText,
                    TimeStamp = DateTime.UtcNow,
                    Author = new Author { Name = $"Other author {Guid.NewGuid():N}" }
                });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/obs/{Uri.EscapeDataString(authorName)}");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(authorName, html);
        Assert.Contains(observationText, html);
        Assert.DoesNotContain(otherObservationText, html);
    }

    [Fact]
    public async Task PublicTimelineFiltersBySelectedTaxonAndItsDescendants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<BisonDBContext>>();
                    services.AddDbContext<BisonDBContext>(
                        options => options.UseSqlite(connection));
                });
            });

        using var client = factory.CreateClient();

        var rootTaxonId = $"test-root-{Guid.NewGuid():N}";

        var selectedTaxon = new Taxon
        {
            DwcTaxonId = rootTaxonId,
            DanishVernacularName = "Test selected taxon"
        };

        var descendantTaxon = new Taxon
        {
            DwcTaxonId = $"test-child-{Guid.NewGuid():N}",
            DanishVernacularName = "Test descendant taxon",
            ParentId = rootTaxonId,
            Parent = selectedTaxon
        };

        var unrelatedTaxon = new Taxon
        {
            DwcTaxonId = $"test-unrelated-{Guid.NewGuid():N}",
            DanishVernacularName = "Test unrelated taxon"
        };

        var selectedText = $"Selected taxon observation {Guid.NewGuid():N}";
        var descendantText = $"Descendant observation {Guid.NewGuid():N}";
        var unrelatedText = $"Unrelated taxon observation {Guid.NewGuid():N}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<BisonDBContext>();

            dbContext.Taxa.AddRange(
                selectedTaxon,
                descendantTaxon,
                unrelatedTaxon);

            dbContext.Observations.AddRange(
                new Observation
                {
                    Text = selectedText,
                    TimeStamp = DateTime.UtcNow,
                    Author = new Author { Name = "Selected taxon author" },
                    Taxon = selectedTaxon
                },
                new Observation
                {
                    Text = descendantText,
                    TimeStamp = DateTime.UtcNow.AddMinutes(-1),
                    Author = new Author { Name = "Descendant author" },
                    Taxon = descendantTaxon
                },
                new Observation
                {
                    Text = unrelatedText,
                    TimeStamp = DateTime.UtcNow.AddMinutes(-2),
                    Author = new Author { Name = "Unrelated author" },
                    Taxon = unrelatedTaxon
                });

            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync(
            $"/obs?taxonId={Uri.EscapeDataString(rootTaxonId)}");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(selectedText, html);
        Assert.Contains(descendantText, html);
        Assert.DoesNotContain(unrelatedText, html);
    }

}
