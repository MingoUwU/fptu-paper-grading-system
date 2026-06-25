using Fptu.Pgs.AiGrading.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.AiGrading.Api.Infrastructure;

public sealed class AiGradingDbContext(DbContextOptions<AiGradingDbContext> options)
    : DbContext(options)
{
    public DbSet<AiGradingResult> AiGradingResults => Set<AiGradingResult>();
    public DbSet<AiCriterionGrade> AiCriterionGrades => Set<AiCriterionGrade>();
    public DbSet<UserAiCredential> UserAiCredentials => Set<UserAiCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("grading");

        modelBuilder.Entity<AiGradingResult>(entity =>
        {
            entity.ToTable("AIGradingResults");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AiScore).HasPrecision(7, 2);
            entity.Property(x => x.MaxScore).HasPrecision(7, 2);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.OverallFeedback).HasMaxLength(4000);
            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.Model).HasMaxLength(100);
            entity.Property(x => x.CredentialSource).HasMaxLength(20);
            entity.HasIndex(x => new { x.SubmissionId, x.GradedAtUtc });
            entity.HasMany(x => x.Criteria)
                .WithOne(x => x.Result)
                .HasForeignKey(x => x.AiGradingResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiCriterionGrade>(entity =>
        {
            entity.ToTable("AICriterionGrades");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CriterionName).HasMaxLength(300);
            entity.Property(x => x.MaxScore).HasPrecision(7, 2);
            entity.Property(x => x.AwardedScore).HasPrecision(7, 2);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.Feedback).HasMaxLength(2000);
            entity.HasIndex(x => new { x.AiGradingResultId, x.CriterionId }).IsUnique();
        });

        modelBuilder.Entity<UserAiCredential>(entity =>
        {
            entity.ToTable("UserAiCredentials");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.ProtectedApiKey).HasMaxLength(4000);
            entity.Property(x => x.MaskedApiKey).HasMaxLength(100);
            entity.HasIndex(x => new { x.TeacherId, x.Provider }).IsUnique();
        });
    }
}
