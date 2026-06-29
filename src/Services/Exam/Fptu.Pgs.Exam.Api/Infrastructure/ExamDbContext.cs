using Fptu.Pgs.Exam.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.Exam.Api.Infrastructure;

public sealed class ExamDbContext(DbContextOptions<ExamDbContext> options)
    : DbContext(options)
{
    public DbSet<ExamDefinition> Exams => Set<ExamDefinition>();
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("exam");

        modelBuilder.Entity<ExamDefinition>(entity =>
        {
            entity.ToTable("Exams");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.SubjectCode).HasMaxLength(50);
            entity.Property(x => x.Semester).HasMaxLength(50);
            entity.Property(x => x.OriginalFileName).HasMaxLength(260);
            entity.Property(x => x.ContentType).HasMaxLength(150);
            entity.Property(x => x.DocumentContent).HasColumnType("varbinary(max)");
            entity.HasMany(x => x.Criteria)
                .WithOne(x => x.Exam)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RubricCriterion>(entity =>
        {
            entity.ToTable("RubricCriteria");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ExamId, x.DisplayOrder }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.AiInstructions).HasMaxLength(4000);
            entity.Property(x => x.MaxScore).HasPrecision(8, 2);
        });
    }
}
