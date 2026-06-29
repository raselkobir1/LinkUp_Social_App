using LinkUp.Modules.Chat.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Chat.Configuration;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Chat> Chats => Set<Entities.Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<GroupChat> GroupChats => Set<GroupChat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageRead> MessageReads => Set<MessageRead>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("chat");

        builder.Entity<Entities.Chat>(e =>
        {
            e.ToTable("Chats");
            e.HasKey(x => x.Id);
        });

        builder.Entity<ChatParticipant>(e =>
        {
            e.ToTable("ChatParticipants");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ChatId, x.UserId });
        });

        builder.Entity<GroupChat>(e =>
        {
            e.ToTable("GroupChats");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChatId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.GroupPhotoUrl).HasMaxLength(500);
        });

        builder.Entity<Message>(e =>
        {
            e.ToTable("Messages");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChatId);
            e.HasIndex(x => x.SenderId);
            e.Property(x => x.Content).HasMaxLength(4000);
        });

        builder.Entity<MessageAttachment>(e =>
        {
            e.ToTable("MessageAttachments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MessageId);
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(100).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255);
        });

        builder.Entity<MessageRead>(e =>
        {
            e.ToTable("MessageReads");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
        });
    }
}
