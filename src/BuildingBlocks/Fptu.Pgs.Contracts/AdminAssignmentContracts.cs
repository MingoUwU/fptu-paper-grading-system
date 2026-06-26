namespace Fptu.Pgs.Contracts;

public enum UserRole
{
    Admin = 1,
    Teacher = 2
}

public enum GradingAssignmentStatus
{
    Assigned = 1,
    InReview = 2,
    TeacherGraded = 3,
    Finalized = 4,
    Cancelled = 5
}

public sealed record SubjectResponse(
    Guid SubjectId,
    string Code,
    string Name,
    bool IsActive);

public sealed record CreateSubjectRequest(
    string Code,
    string Name);

public sealed record AssignTeacherToSubjectRequest(
    Guid TeacherId,
    string SubjectCode,
    Guid AssignedByAdminId);

public sealed record TeacherSubjectAssignmentResponse(
    Guid AssignmentId,
    Guid TeacherId,
    string SubjectCode,
    Guid AssignedByAdminId,
    DateTimeOffset AssignedAtUtc,
    bool IsActive);

public sealed record CreateGradingAssignmentRequest(
    Guid SubmissionId,
    Guid ExamId,
    Guid TeacherId,
    Guid AssignedByAdminId,
    DateTimeOffset? DueAtUtc);

public sealed record BulkAssignSubmissionsRequest(
    Guid ExamId,
    Guid TeacherId,
    Guid AssignedByAdminId,
    IReadOnlyCollection<Guid> SubmissionIds,
    DateTimeOffset? DueAtUtc);

public sealed record AutoDistributeAssignmentsRequest(
    Guid ExamId,
    Guid AssignedByAdminId,
    IReadOnlyCollection<Guid> TeacherIds,
    IReadOnlyCollection<Guid> SubmissionIds,
    DateTimeOffset? DueAtUtc);

public sealed record GradingAssignmentResponse(
    Guid AssignmentId,
    Guid SubmissionId,
    Guid ExamId,
    Guid TeacherId,
    Guid AssignedByAdminId,
    GradingAssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? TeacherGradedAtUtc,
    DateTimeOffset? FinalizedAtUtc);

public sealed record TeacherWorkItemResponse(
    Guid AssignmentId,
    Guid SubmissionId,
    Guid ExamId,
    Guid TeacherId,
    GradingAssignmentStatus AssignmentStatus,
    SubmissionStatus? ScoreStatus,
    decimal? AiScore,
    decimal? TeacherScore,
    decimal? FinalScore,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? DueAtUtc);
