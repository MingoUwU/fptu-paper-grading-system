using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.ReviewScore.Api.Domain;

public sealed class GradingAssignment
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid AssignedByAdminId { get; set; }
    public GradingAssignmentStatus Status { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? TeacherGradedAtUtc { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }

    public static GradingAssignment Create(
        Guid submissionId,
        Guid examId,
        Guid teacherId,
        Guid assignedByAdminId,
        DateTimeOffset? dueAtUtc)
    {
        if (submissionId == Guid.Empty ||
            examId == Guid.Empty ||
            teacherId == Guid.Empty ||
            assignedByAdminId == Guid.Empty)
        {
            throw new ScoreDomainException(
                "SubmissionId, ExamId, TeacherId and AssignedByAdminId are required.");
        }

        return new GradingAssignment
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            ExamId = examId,
            TeacherId = teacherId,
            AssignedByAdminId = assignedByAdminId,
            Status = GradingAssignmentStatus.Assigned,
            AssignedAtUtc = DateTimeOffset.UtcNow,
            DueAtUtc = dueAtUtc
        };
    }

    public void EnsureTeacher(Guid teacherId)
    {
        if (TeacherId != teacherId)
        {
            throw new ScoreDomainException(
                "This submission is assigned to another teacher.");
        }
    }

    public void MarkInReview(Guid teacherId)
    {
        EnsureTeacher(teacherId);

        if (Status is GradingAssignmentStatus.Assigned)
        {
            Status = GradingAssignmentStatus.InReview;
            StartedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkTeacherGraded(Guid teacherId)
    {
        EnsureTeacher(teacherId);

        if (Status is GradingAssignmentStatus.Finalized)
        {
            throw new ScoreDomainException(
                "A finalized assignment cannot be graded again.");
        }

        if (Status is GradingAssignmentStatus.Cancelled)
        {
            throw new ScoreDomainException(
                "A cancelled assignment cannot be graded.");
        }

        StartedAtUtc ??= DateTimeOffset.UtcNow;
        TeacherGradedAtUtc = DateTimeOffset.UtcNow;
        Status = GradingAssignmentStatus.TeacherGraded;
    }

    public void MarkFinalized(Guid teacherId)
    {
        EnsureTeacher(teacherId);

        if (Status is GradingAssignmentStatus.Cancelled)
        {
            throw new ScoreDomainException(
                "A cancelled assignment cannot be finalized.");
        }

        StartedAtUtc ??= DateTimeOffset.UtcNow;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
        Status = GradingAssignmentStatus.Finalized;
    }

    public void Cancel(Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            throw new ScoreDomainException("AdminId is required.");
        }

        if (Status is GradingAssignmentStatus.Finalized)
        {
            throw new ScoreDomainException(
                "A finalized assignment cannot be cancelled.");
        }

        Status = GradingAssignmentStatus.Cancelled;
    }
}
