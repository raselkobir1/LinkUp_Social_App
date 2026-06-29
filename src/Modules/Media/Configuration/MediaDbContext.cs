using LinkUp.Modules.Media.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Media.Configuration;

public class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("media");

        builder.Entity<MediaFile>(e =>
        {
            e.ToTable("MediaFiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.PublicId).HasMaxLength(500).IsRequired();
            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            e.Property(x => x.Format).HasMaxLength(50);
            e.Property(x => x.Folder).HasMaxLength(200).IsRequired();
        });
    }
}
