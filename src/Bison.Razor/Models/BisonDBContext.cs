using Microsoft.EntityFrameworkCore;

namespace Bison.Razor.Models;

public class BisonDBContext : DbContext
{
    public BisonDBContext(DbContextOptions<BisonDBContext> options) : base(options) { }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Taxon> Taxa => Set<Taxon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>().HasDiscriminator<string>("Discriminator")
            .HasValue<Observation>("Observation")
            .HasValue<Comment>("Comment")
            .HasValue<Proposal>("Proposal");

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Author)
            .WithMany(a => a.Posts)
            .HasForeignKey(p => p.AuthorId);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Observation)
            .WithMany(o => o.Comments)
            .HasForeignKey(c => c.ObservationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Proposal>()
            .HasOne(p => p.Observation)
            .WithMany(o => o.Proposals)
            .HasForeignKey(p => p.ObservationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Observation>()
            .HasOne(o => o.Taxon)
            .WithMany()
            .HasForeignKey(o => o.TaxonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Proposal>()
            .HasOne(p => p.Taxon)
            .WithMany()
            .HasForeignKey(p => p.TaxonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Taxon>(t =>
        {
            t.HasKey(x => x.DwcTaxonId);
            t.HasOne(x => x.Parent)
             .WithMany(x => x.Children)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
