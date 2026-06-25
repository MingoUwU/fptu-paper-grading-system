namespace Fptu.Pgs.Contracts;

public sealed record RubricCriterionInput(
    Guid CriterionId,
    string Name,
    string Description,
    decimal MaxScore);

public sealed record GradeSubmissionRequest(
    Guid SubmissionId,
    Guid ExamId,
    Guid TeacherId,
    string? ExtractedText,
    string? PdfBase64,
    IReadOnlyCollection<RubricCriterionInput> Criteria,
    string? Provider = null);

public sealed record AiCriterionGradeResponse(
    Guid CriterionId,
    string CriterionName,
    decimal MaxScore,
    decimal AwardedScore,
    IReadOnlyCollection<string> Evidence,
    IReadOnlyCollection<string> MissingPoints,
    string Feedback,
    decimal Confidence);

public sealed record AiGradingResultResponse(
    Guid ResultId,
    Guid SubmissionId,
    Guid ExamId,
    decimal AiScore,
    decimal MaxScore,
    string OverallFeedback,
    decimal Confidence,
    string Provider,
    string Model,
    string CredentialSource,
    SubmissionStatus Status,
    DateTimeOffset GradedAtUtc,
    IReadOnlyCollection<AiCriterionGradeResponse> Criteria,
    bool ReviewScoreSynchronized);

public sealed record RegisterAiGradeRequest(
    Guid AiGradingResultId,
    Guid SubmissionId,
    Guid ExamId,
    decimal AiScore,
    decimal MaxScore,
    string AiFeedback,
    decimal AiConfidence,
    string AiProvider,
    string AiModel,
    string AiCredentialSource,
    DateTimeOffset AiGradedAtUtc,
    IReadOnlyCollection<AiCriterionGradeResponse> Criteria);

public sealed record TeacherCriterionGradeRequest(
    Guid CriterionId,
    decimal Score,
    string? Feedback);

public sealed record SubmitTeacherGradeRequest(
    Guid TeacherId,
    decimal Score,
    string? Feedback,
    IReadOnlyCollection<TeacherCriterionGradeRequest> Criteria);

public sealed record FinalizeScoreRequest(Guid TeacherId);

public sealed record CriterionScoreComparisonResponse(
    Guid CriterionId,
    string CriterionName,
    decimal MaxScore,
    decimal AiScore,
    string AiFeedback,
    decimal? TeacherScore,
    string? TeacherFeedback);

public sealed record ScoreComparisonResponse(
    Guid ScoreId,
    Guid SubmissionId,
    Guid ExamId,
    decimal AiScore,
    decimal MaxScore,
    string AiFeedback,
    decimal AiConfidence,
    string AiProvider,
    string AiModel,
    string AiCredentialSource,
    decimal? TeacherScore,
    string? TeacherFeedback,
    decimal? Difference,
    decimal? FinalScore,
    SubmissionStatus Status,
    DateTimeOffset AiGradedAtUtc,
    DateTimeOffset? TeacherGradedAtUtc,
    DateTimeOffset? FinalizedAtUtc,
    IReadOnlyCollection<CriterionScoreComparisonResponse> Criteria);

public sealed record SaveAiCredentialRequest(
    string Provider,
    string ApiKey,
    bool AllowSystemFallback);

public sealed record AiCredentialStatusResponse(
    Guid TeacherId,
    string Provider,
    bool HasCredential,
    string? MaskedApiKey,
    bool AllowSystemFallback,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? LastValidatedAtUtc);

public sealed record AiCredentialValidationResponse(
    bool IsValid,
    string Provider,
    string Message,
    DateTimeOffset CheckedAtUtc);
