namespace Fptu.Pgs.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record UserAccountResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string? SubjectCode,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);

public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    UserRole Role,
    string? SubjectCode);

public sealed record UpdateUserRequest(
    string FullName,
    UserRole Role,
    string? SubjectCode);

public sealed record SetUserStatusRequest(bool IsActive);

public sealed record ResetUserPasswordRequest(string NewPassword);

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
