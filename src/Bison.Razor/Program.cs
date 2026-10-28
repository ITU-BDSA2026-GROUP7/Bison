using Bison.Razor.Data;
using Bison.Razor.Models;
using Bison.Razor.Services;
using Bison.Razor.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();


var efDbPath = Environment.GetEnvironmentVariable("BISON_EF_DBPATH")
    ?? Path.Combine(Path.GetTempPath(), "bison-ef.db");
builder.Services.AddDbContext<BisonDBContext>(options =>
    options.UseSqlite($"Data Source={efDbPath}"));


// ASP.NET Core Identity: login/register/logout, backed by BisonDBContext.
builder.Services.AddDefaultIdentity<Author>(options =>
    {
        // No email sending is set up, so don't require email confirmation.
        options.SignIn.RequireConfirmedAccount = false;

        // Relaxed rules so the required demo passwords "123456" and
        // "password" are accepted. Far too weak for a real application.
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<BisonDBContext>();

builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IObservationService, ObservationService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<ITaxonFilterService, TaxonFilterService>();


var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BisonDBContext>();
    await dbContext.Database.EnsureCreatedAsync();
    DbInitializer.SeedDatabase(dbContext);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Author>>();
    await IdentityInitializer.SeedUsersAsync(userManager);
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

// Order matters: first find out WHO the user is, then check what they may do.
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// There is no page at "/", but Identity sends users there after logging in
// or out, and the handout expects Public Observations at "/". Redirect it.
app.MapGet("/", () => Results.Redirect("/obs"));

app.Run();

public partial class Program
{
}