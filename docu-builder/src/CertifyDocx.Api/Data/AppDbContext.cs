using Microsoft.EntityFrameworkCore;

namespace CertifyDocx.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Template> Templates => Set<Template>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Template>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(t => t.Name).IsUnique();
            entity.Property(t => t.FileBytes).IsRequired();
            entity.Property(t => t.SchemaJson).IsRequired();
        });
    }
}
