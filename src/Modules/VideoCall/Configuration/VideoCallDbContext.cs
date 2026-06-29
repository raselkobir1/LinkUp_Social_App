using LinkUp.Modules.VideoCall.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.VideoCall.Configuration;

public class VideoCallDbContext(DbContextOptions<VideoCallDbContext> options) : DbContext(options)
{
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<CallParticipant> CallParticipants => Set<CallParticipant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("videocall");

        builder.Entity<Call>(e =>
        {
            e.ToTable("Calls");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InitiatedById);
            e.HasIndex(x => x.Status);

            e.HasMany(x => x.Participants)
             .WithOne(x => x.Call)
             .HasForeignKey(x => x.CallId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CallParticipant>(e =>
        {
            e.ToTable("CallParticipants");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CallId);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });
    }
}
