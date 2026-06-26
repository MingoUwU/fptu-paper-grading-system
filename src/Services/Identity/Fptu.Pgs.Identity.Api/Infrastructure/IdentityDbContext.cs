using Fptu.Pgs.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.Identity.Api.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.SubjectCode).HasMaxLength(50);
            entity.Property(x => x.PasswordSalt).HasMaxLength(128);
            entity.Property(x => x.PasswordHash).HasMaxLength(256);
        });
    }
}
