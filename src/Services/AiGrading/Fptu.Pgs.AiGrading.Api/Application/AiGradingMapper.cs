using System.Text.Json;
using Fptu.Pgs.AiGrading.Api.Domain;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Application;

public static class AiGradingMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AiGradingResult ToEntity(
        GradeSubmissionRequest request,
        ProviderGradingResult providerResult)
    {
        var resultId = Guid.NewGuid();

        return new AiGradingResult
        {
            Id = resultId,
            SubmissionId = request.SubmissionId,
            ExamId = request.ExamId,
            AiScore = providerResult.TotalScore,
            MaxScore = providerResult.MaxScore,
            OverallFeedback = providerResult.OverallFeedback,
            Confidence = providerResult.Confidence,
            Provider = providerResult.Provider,
            Model = providerResult.Model,
            CredentialSource = providerResult.CredentialSource,
            Status = SubmissionStatus.AiGraded,
            GradedAtUtc = DateTimeOffset.UtcNow,
            Criteria = providerResult.Criteria.Select(criterion => new AiCriterionGrade
            {
                Id = Guid.NewGuid(),
                AiGradingResultId = resultId,
                CriterionId = criterion.CriterionId,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore,
                AwardedScore = criterion.AwardedScore,
                EvidenceJson = JsonSerializer.Serialize(criterion.Evidence, JsonOptions),
                MissingPointsJson = JsonSerializer.Serialize(criterion.MissingPoints, JsonOptions),
                Feedback = criterion.Feedback,
                Confidence = criterion.Confidence
            }).ToList()
        };
    }

    public static AiGradingResultResponse ToResponse(this AiGradingResult result) =>
        new(
            result.Id,
            result.SubmissionId,
            result.ExamId,
            result.AiScore,
            result.MaxScore,
            result.OverallFeedback,
            result.Confidence,
            result.Provider,
            result.Model,
            result.CredentialSource,
            result.Status,
            result.GradedAtUtc,
            result.Criteria.Select(ToResponse).ToArray(),
            result.ReviewScoreSynchronized);

    public static RegisterAiGradeRequest ToRegisterRequest(this AiGradingResult result) =>
        new(
            result.Id,
            result.SubmissionId,
            result.ExamId,
            result.AiScore,
            result.MaxScore,
            result.OverallFeedback,
            result.Confidence,
            result.Provider,
            result.Model,
            result.CredentialSource,
            result.GradedAtUtc,
            result.Criteria.Select(ToResponse).ToArray());

    private static AiCriterionGradeResponse ToResponse(AiCriterionGrade criterion) =>
        new(
            criterion.CriterionId,
            criterion.CriterionName,
            criterion.MaxScore,
            criterion.AwardedScore,
            DeserializeList(criterion.EvidenceJson),
            DeserializeList(criterion.MissingPointsJson),
            criterion.Feedback,
            criterion.Confidence);

    private static IReadOnlyCollection<string> DeserializeList(string json) =>
        JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
}
