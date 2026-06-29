namespace Fptu.Pgs.Contracts;

public enum RubricStatus
{
    Draft = 1,
    Published = 2
}

public sealed record ExamSummaryResponse(
    Guid ExamId,
    string Code,
    string Name,
    string SubjectCode,
    string Semester,
    string OriginalFileName,
    RubricStatus RubricStatus,
    int CriterionCount,
    decimal TotalScore,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc);

public sealed record RubricCriterionResponse(
    Guid CriterionId,
    string Name,
    string Description,
    string AiInstructions,
    decimal MaxScore,
    int DisplayOrder);

public sealed record ExamRubricResponse(
    Guid ExamId,
    string ExamCode,
    string ExamName,
    string SubjectCode,
    string Semester,
    string OriginalFileName,
    RubricStatus Status,
    decimal TotalScore,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyCollection<RubricCriterionResponse> Criteria);

public sealed record UpsertRubricCriterionRequest(
    Guid? CriterionId,
    string Name,
    string Description,
    string AiInstructions,
    decimal MaxScore,
    int DisplayOrder);

public sealed record SaveExamRubricRequest(
    IReadOnlyCollection<UpsertRubricCriterionRequest> Criteria);

public sealed record GradeSubmissionFromExamRequest(
    Guid SubmissionId,
    Guid ExamId,
    Guid TeacherId,
    string? ExtractedText,
    string? PdfBase64,
    string? Provider = null);
