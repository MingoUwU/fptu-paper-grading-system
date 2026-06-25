namespace Fptu.Pgs.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    string Role);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record CreateExamRequest(
    string Code,
    string Name,
    string SubjectCode,
    string Semester);

public sealed record AddRubricCriterionRequest(
    string Name,
    string Description,
    decimal MaxScore);

public sealed record CreateOcrJobRequest(Guid SubmissionId, bool Force = false);

public sealed record CreateAiGradingJobRequest(Guid SubmissionId, Guid ExamId);

public sealed record UpdateScoreRequest(decimal Score, string? Feedback);

public sealed record FinalizeScoreRequest(
    Guid SubmissionId,
    decimal Score,
    string? Feedback);

public sealed record BatchUploadResponse(
    Guid BatchId,
    int FileCount,
    SubmissionStatus Status);

public sealed record JobStatusResponse(
    Guid JobId,
    Guid SubmissionId,
    SubmissionStatus Status,
    int ProgressPercent,
    string? Error);
