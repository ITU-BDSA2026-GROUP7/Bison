using Bison.Core.Repositories;
using Bison.Infrastructure.Persistence;
using Bison.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bison.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBisonInfrastructure(this IServiceCollection services)
    {
        var efDbPath = Environment.GetEnvironmentVariable("BISON_EF_DBPATH")
            ?? Path.Combine(Path.GetTempPath(), "bison-ef.db");

        services.AddDbContext<BisonDBContext>(options =>
            options.UseSqlite($"Data Source={efDbPath}"));

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITaxonRepository, TaxonRepository>();

        return services;
    }
}
