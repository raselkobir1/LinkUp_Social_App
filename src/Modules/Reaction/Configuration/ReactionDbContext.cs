using LinkUp.Modules.Reaction.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Reaction.Configuration;

public class ReactionDbContext(DbContextOptions<ReactionDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Reaction> Reactions => Set<Entities.Reaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("reaction");

        builder.Entity<Entities.Reaction>(e =>
        {
            e.ToTable("reactions");
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.UserId, r.TargetId, r.TargetType }).IsUnique();
            e.Property(r => r.TargetType).HasMaxLength(20).IsRequired();
        });
    }
}
