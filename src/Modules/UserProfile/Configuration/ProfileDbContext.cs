using LinkUp.Modules.UserProfile.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.UserProfile.Configuration;

public class ProfileDbContext(DbContextOptions<ProfileDbContext> options) : DbContext(options)
{
    public DbSet<Entities.UserProfile> Profiles => Set<Entities.UserProfile>();
    public DbSet<UserEducation> UserEducations => Set<UserEducation>();
    public DbSet<UserExperience> UserExperiences => Set<UserExperience>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<PrivacySettings> PrivacySettings => Set<PrivacySettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("profile");

        builder.Entity<Entities.UserProfile>(e =>
        {
            e.ToTable("Profiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.Bio).HasMaxLength(500);
            e.Property(x => x.Gender).HasMaxLength(50);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Website).HasMaxLength(500);
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
            e.Property(x => x.CoverPhotoUrl).HasMaxLength(500);
        });

        builder.Entity<UserEducation>(e =>
        {
            e.ToTable("UserEducations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.School).HasMaxLength(200).IsRequired();
            e.Property(x => x.Degree).HasMaxLength(200);
            e.Property(x => x.FieldOfStudy).HasMaxLength(200);
        });

        builder.Entity<UserExperience>(e =>
        {
            e.ToTable("UserExperiences");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Company).HasMaxLength(200).IsRequired();
            e.Property(x => x.Position).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        builder.Entity<SocialLink>(e =>
        {
            e.ToTable("SocialLinks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
        });

        builder.Entity<PrivacySettings>(e =>
        {
            e.ToTable("PrivacySettings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
        });
    }
}
