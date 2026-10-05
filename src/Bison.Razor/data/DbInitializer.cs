using Bison.Razor.Models;
using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(BisonDBContext dbContext)
    {
        if (await dbContext.Observations.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var heronTaxon = new Taxon
        {
            DwcTaxonId = "demo-heron",
            DanishVernacularName = "Fiskehejre"
        };
        var heronObservation = new Observation
        {
            Text = "A heron standing by the lake",
            TimeStamp = now.AddMinutes(-45),
            Author = new Author { Name = "Alex", Email = "alex@example.test" }
        };
        var foxObservation = new Observation
        {
            Text = "Fresh tracks spotted near the forest",
            TimeStamp = now.AddMinutes(-30),
            Author = new Author { Name = "Sam", Email = "sam@example.test" }
        };
        var flowerObservation = new Observation
        {
            Text = "The first wildflowers are blooming",
            TimeStamp = now.AddMinutes(-15),
            Author = new Author { Name = "Jamie", Email = "jamie@example.test" }
        };

        dbContext.Taxa.Add(heronTaxon);
        dbContext.Observations.AddRange(heronObservation, foxObservation, flowerObservation);
        dbContext.Comments.AddRange(
            new Comment
            {
                Text = "Great find! Was it near the reeds?",
                TimeStamp = now.AddMinutes(-40),
                Author = new Author { Name = "Sam", Email = "sam@example.test" },
                Observation = heronObservation
            },
            new Comment
            {
                Text = "I saw similar tracks there yesterday.",
                TimeStamp = now.AddMinutes(-25),
                Author = new Author { Name = "Alex", Email = "alex@example.test" },
                Observation = foxObservation
            });
        dbContext.Proposals.AddRange(
            new Proposal
            {
                Text = "This looks like a grey heron.",
                TimeStamp = now.AddMinutes(-35),
                Author = new Author { Name = "Jamie", Email = "jamie@example.test" },
                Observation = heronObservation,
                Taxon = heronTaxon
            },
            new Proposal
            {
                Text = "Possibly a red fox.",
                TimeStamp = now.AddMinutes(-20),
                Author = new Author { Name = "Sam", Email = "sam@example.test" },
                Observation = foxObservation,
                Taxon = new Taxon
                {
                    DwcTaxonId = "demo-red-fox",
                    DanishVernacularName = "Ræv"
                }
            });

        await dbContext.SaveChangesAsync();
    }
}
