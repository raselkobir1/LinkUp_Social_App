using Microsoft.EntityFrameworkCore;
using NotificationEntity = LinkUp.Modules.Notification.Entities.Notification;

namespace LinkUp.Modules.Notification.Configuration;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("notification");

        builder.Entity<NotificationEntity>(e =>
        {
            e.ToTable("Notifications");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RecipientId);
            e.HasIndex(x => new { x.RecipientId, x.IsRead });
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100);
        });
    }
}
