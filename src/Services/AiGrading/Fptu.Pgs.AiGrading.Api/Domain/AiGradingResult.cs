using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Domain;

public sealed class AiGradingResult
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public decimal AiScore { get; set; }
    public decimal MaxScore { get; set; }
    public string OverallFeedback { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string CredentialSource { get; set; } = "None";
    public SubmissionStatus Status { get; set; }
    public DateTimeOffset GradedAtUtc { get; set; }
    public bool ReviewScoreSynchronized { get; set; }
    public List<AiCriterionGrade> Criteria { get; set; } = [];
}

public sealed class AiCriterionGrade
{
    public Guid Id { get; set; }
    public Guid AiGradingResultId { get; set; }
    public Guid CriterionId { get; set; }
    public string CriterionName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal AwardedScore { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public string MissingPointsJson { get; set; } = "[]";
    public string Feedback { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public AiGradingResult Result { get; set; } = null!;
}

public sealed class UserAiCredential
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProtectedApiKey { get; set; } = string.Empty;
    public string MaskedApiKey { get; set; } = string.Empty;
    public bool AllowSystemFallback { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? LastValidatedAtUtc { get; set; }
}
