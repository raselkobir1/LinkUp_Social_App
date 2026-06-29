using LinkUp.Modules.Identity.Entities;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Identity.Configuration;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>(u =>
        {
            u.ToTable("Users");
            u.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            u.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            u.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
            u.Property(x => x.CoverPhotoUrl).HasMaxLength(500);
        });

        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<RefreshToken>(rt =>
        {
            rt.ToTable("RefreshTokens");
            rt.HasKey(x => x.Id);
            rt.Property(x => x.Token).HasMaxLength(500).IsRequired();
            rt.HasOne(x => x.User)
              .WithMany(u => u.RefreshTokens)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Cascade);
            rt.HasIndex(x => x.Token).IsUnique();
        });

        SeedRoles(builder);
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        var adminRoleId = Guid.Parse("8d04dce2-969a-435d-bba4-df3f325983dc");
        var userRoleId = Guid.Parse("4b162b3e-4e38-45c3-8697-25c5d5e3c3d5");

        builder.Entity<ApplicationRole>().HasData(
            new ApplicationRole { Id = adminRoleId, Name = AppConstants.Roles.Admin, NormalizedName = AppConstants.Roles.Admin.ToUpper(), ConcurrencyStamp = "1167b627-534b-443e-a1a7-e53e146b0937" },
            new ApplicationRole { Id = userRoleId, Name = AppConstants.Roles.User, NormalizedName = AppConstants.Roles.User.ToUpper(), ConcurrencyStamp = "8b1f7cae-ff70-4823-a58d-86c4f9618ae6" }
        );
    }
}
