using Fptu.Pgs.ReviewScore.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.ReviewScore.Api.Infrastructure;

public sealed class ReviewScoreDbContext(DbContextOptions<ReviewScoreDbContext> options)
    : DbContext(options)
{
    public DbSet<SubmissionScore> SubmissionScores => Set<SubmissionScore>();
    public DbSet<CriterionScore> CriterionScores => Set<CriterionScore>();
    public DbSet<ScoreAuditLog> ScoreAuditLogs => Set<ScoreAuditLog>();
    public DbSet<GradingAssignment> GradingAssignments => Set<GradingAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("score");

        modelBuilder.Entity<SubmissionScore>(entity =>
        {
            entity.ToTable("SubmissionScores");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SubmissionId).IsUnique();
            entity.HasIndex(x => x.AiGradingResultId).IsUnique();
            entity.Property(x => x.AiScore).HasPrecision(7, 2);
            entity.Property(x => x.MaxScore).HasPrecision(7, 2);
            entity.Property(x => x.AiConfidence).HasPrecision(5, 4);
            entity.Property(x => x.TeacherScore).HasPrecision(7, 2);
            entity.Property(x => x.FinalScore).HasPrecision(7, 2);
            entity.Property(x => x.AiFeedback).HasMaxLength(4000);
            entity.Property(x => x.TeacherFeedback).HasMaxLength(4000);
            entity.Property(x => x.AiProvider).HasMaxLength(50);
            entity.Property(x => x.AiModel).HasMaxLength(100);
            entity.Property(x => x.AiCredentialSource).HasMaxLength(20);
            entity.HasMany(x => x.Criteria)
                .WithOne(x => x.SubmissionScore)
                .HasForeignKey(x => x.SubmissionScoreId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.AuditLogs)
                .WithOne(x => x.SubmissionScore)
                .HasForeignKey(x => x.SubmissionScoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CriterionScore>(entity =>
        {
            entity.ToTable("CriterionScores");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CriterionName).HasMaxLength(300);
            entity.Property(x => x.MaxScore).HasPrecision(7, 2);
            entity.Property(x => x.AiScore).HasPrecision(7, 2);
            entity.Property(x => x.TeacherScore).HasPrecision(7, 2);
            entity.Property(x => x.AiFeedback).HasMaxLength(2000);
            entity.Property(x => x.TeacherFeedback).HasMaxLength(2000);
            entity.HasIndex(x => new { x.SubmissionScoreId, x.CriterionId }).IsUnique();
        });

        modelBuilder.Entity<ScoreAuditLog>(entity =>
        {
            entity.ToTable("ScoreAuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.OldScore).HasPrecision(7, 2);
            entity.Property(x => x.NewScore).HasPrecision(7, 2);
            entity.HasIndex(x => new { x.SubmissionScoreId, x.OccurredAtUtc });
        });

        modelBuilder.Entity<GradingAssignment>(entity =>
        {
            entity.ToTable("GradingAssignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SubmissionId).IsUnique();
            entity.HasIndex(x => new { x.TeacherId, x.Status });
            entity.HasIndex(x => new { x.ExamId, x.TeacherId });
        });
    }
}
