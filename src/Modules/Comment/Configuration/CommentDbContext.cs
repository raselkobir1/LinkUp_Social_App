using Microsoft.EntityFrameworkCore;
using CommentEntity = LinkUp.Modules.Comment.Entities.Comment;
using CommentLikeEntity = LinkUp.Modules.Comment.Entities.CommentLike;

namespace LinkUp.Modules.Comment.Configuration;

public class CommentDbContext(DbContextOptions<CommentDbContext> options) : DbContext(options)
{
    public DbSet<CommentEntity> Comments => Set<CommentEntity>();
    public DbSet<CommentLikeEntity> CommentLikes => Set<CommentLikeEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("comment");

        builder.Entity<CommentEntity>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.LikeCount).HasDefaultValue(0);
            e.Property(x => x.ReplyCount).HasDefaultValue(0);
            e.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            e.HasMany(x => x.Likes)
                .WithOne(x => x.Comment)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommentLikeEntity>(e =>
        {
            e.ToTable("comment_likes");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CommentId, x.UserId }).IsUnique();
        });
    }
}
