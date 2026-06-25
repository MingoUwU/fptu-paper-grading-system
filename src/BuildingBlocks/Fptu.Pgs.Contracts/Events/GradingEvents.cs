namespace Fptu.Pgs.Contracts.Events;

public sealed record BatchUploaded(
    Guid BatchId,
    IReadOnlyCollection<Guid> SubmissionIds,
    DateTimeOffset OccurredAtUtc);

public sealed record OcrCompleted(
    Guid SubmissionId,
    Guid OcrResultId,
    DateTimeOffset OccurredAtUtc);

public sealed record OcrFailed(
    Guid SubmissionId,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset OccurredAtUtc);

public sealed record AiGradingCompleted(
    Guid SubmissionId,
    Guid AiGradingResultId,
    decimal AiScore,
    decimal MaxScore,
    decimal Confidence,
    DateTimeOffset OccurredAtUtc);

public sealed record AiGradingFailed(
    Guid SubmissionId,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset OccurredAtUtc);

public sealed record TeacherGradingCompleted(
    Guid SubmissionId,
    Guid ScoreId,
    Guid TeacherId,
    decimal AiScore,
    decimal TeacherScore,
    DateTimeOffset OccurredAtUtc);

public sealed record ScoreFinalized(
    Guid SubmissionId,
    Guid FinalScoreId,
    decimal Score,
    Guid FinalizedBy,
    DateTimeOffset OccurredAtUtc);
