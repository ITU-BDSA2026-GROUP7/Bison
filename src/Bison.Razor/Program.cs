using Bison.Razor.Data;
using Bison.Razor.Models;
using Bison.Razor.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();


var efDbPath = Environment.GetEnvironmentVariable("BISON_EF_DBPATH")
    ?? Path.Combine(Path.GetTempPath(), "bison-ef.db");
builder.Services.AddDbContext<BisonDBContext>(options =>
    options.UseSqlite($"Data Source={efDbPath}"));


builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IObservationService, ObservationService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IProposalService, ProposalService>();


var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BisonDBContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

public partial class Program
{
}