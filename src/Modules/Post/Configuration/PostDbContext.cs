using Microsoft.EntityFrameworkCore;
using PostEntity = LinkUp.Modules.Post.Entities.Post;
using PostImageEntity = LinkUp.Modules.Post.Entities.PostImage;
using PostVideoEntity = LinkUp.Modules.Post.Entities.PostVideo;
using PostReportEntity = LinkUp.Modules.Post.Entities.PostReport;

namespace LinkUp.Modules.Post.Configuration;

public class PostDbContext(DbContextOptions<PostDbContext> options) : DbContext(options)
{
    public DbSet<PostEntity> Posts => Set<PostEntity>();
    public DbSet<PostImageEntity> PostImages => Set<PostImageEntity>();
    public DbSet<PostVideoEntity> PostVideos => Set<PostVideoEntity>();
    public DbSet<PostReportEntity> Reports => Set<PostReportEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("post");

        builder.Entity<PostEntity>(e =>
        {
            e.ToTable("posts");
            e.HasKey(x => x.Id);

            e.Property(x => x.Content).HasMaxLength(5000);
            e.Property(x => x.IsPinned).HasDefaultValue(false);
            e.Property(x => x.ShareCount).HasDefaultValue(0);
            e.Property(x => x.CommentCount).HasDefaultValue(0);
            e.Property(x => x.ReactionCount).HasDefaultValue(0);

            e.Property(x => x.PostType).IsRequired();
            e.Property(x => x.Visibility).IsRequired();

            e.HasMany(x => x.Images)
                .WithOne(x => x.Post)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Video)
                .WithOne(x => x.Post)
                .HasForeignKey<PostVideoEntity>(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.OriginalPost)
                .WithMany()
                .HasForeignKey(x => x.OriginalPostId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.AuthorId);
            e.HasIndex(x => x.WallUserId);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.IsDeleted, x.CreatedAt });
        });

        builder.Entity<PostImageEntity>(e =>
        {
            e.ToTable("post_images");
            e.HasKey(x => x.Id);

            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            e.Property(x => x.DisplayOrder).HasDefaultValue(0);

            e.HasIndex(x => x.PostId);
        });

        builder.Entity<PostVideoEntity>(e =>
        {
            e.ToTable("post_videos");
            e.HasKey(x => x.Id);

            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);

            e.HasIndex(x => x.PostId).IsUnique();
        });

        builder.Entity<PostReportEntity>(e =>
        {
            e.ToTable("post_reports");
            e.HasKey(x => x.Id);

            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.IsResolved).HasDefaultValue(false);

            e.HasOne(x => x.Post)
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.IsResolved);
        });
    }
}
