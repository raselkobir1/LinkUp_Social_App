using LinkUp.Modules.Friend.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Friend.Configuration;

public class FriendDbContext(DbContextOptions<FriendDbContext> options) : DbContext(options)
{
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("friend");

        builder.Entity<FriendRequest>(e =>
        {
            e.ToTable("FriendRequests");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SenderId, x.ReceiverId });
            e.Property(x => x.SenderName).HasMaxLength(200).IsRequired();
            e.Property(x => x.SenderProfilePictureUrl).HasMaxLength(500);
            e.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ReceiverProfilePictureUrl).HasMaxLength(500);
        });

        builder.Entity<Friendship>(e =>
        {
            e.ToTable("Friendships");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.FriendId }).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(200).IsRequired();
            e.Property(x => x.UserProfilePictureUrl).HasMaxLength(500);
            e.Property(x => x.FriendName).HasMaxLength(200).IsRequired();
            e.Property(x => x.FriendProfilePictureUrl).HasMaxLength(500);
        });

        builder.Entity<BlockedUser>(e =>
        {
            e.ToTable("BlockedUsers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BlockerId, x.BlockedId }).IsUnique();
            e.Property(x => x.BlockedName).HasMaxLength(200).IsRequired();
            e.Property(x => x.BlockedProfilePictureUrl).HasMaxLength(500);
        });
    }
}
