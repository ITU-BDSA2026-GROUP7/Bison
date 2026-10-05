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
}
