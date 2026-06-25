using Fptu.Pgs.Contracts;
using Fptu.Pgs.ReviewScore.Api.Domain;

namespace Fptu.Pgs.ReviewScore.Api.Application;

public static class ScoreMapper
{
    public static SubmissionScore FromAiGrade(RegisterAiGradeRequest request)
    {
        var scoreId = Guid.NewGuid();

        return new SubmissionScore
        {
            Id = scoreId,
            SubmissionId = request.SubmissionId,
            ExamId = request.ExamId,
            AiGradingResultId = request.AiGradingResultId,
            AiScore = request.AiScore,
            MaxScore = request.MaxScore,
            AiFeedback = request.AiFeedback,
            AiConfidence = request.AiConfidence,
            AiProvider = request.AiProvider,
            AiModel = request.AiModel,
            AiCredentialSource = request.AiCredentialSource,
            AiGradedAtUtc = request.AiGradedAtUtc,
            Status = SubmissionStatus.AiGraded,
            Criteria = request.Criteria.Select(criterion => new CriterionScore
            {
                Id = Guid.NewGuid(),
                SubmissionScoreId = scoreId,
                CriterionId = criterion.CriterionId,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore,
                AiScore = criterion.AwardedScore,
                AiFeedback = criterion.Feedback
            }).ToList()
        };
    }

    public static ScoreComparisonResponse ToResponse(this SubmissionScore score) =>
        new(
            score.Id,
            score.SubmissionId,
            score.ExamId,
            score.AiScore,
            score.MaxScore,
            score.AiFeedback,
            score.AiConfidence,
            score.AiProvider,
            score.AiModel,
            score.AiCredentialSource,
            score.TeacherScore,
            score.TeacherFeedback,
            score.TeacherScore.HasValue ? score.TeacherScore.Value - score.AiScore : null,
            score.FinalScore,
            score.Status,
            score.AiGradedAtUtc,
            score.TeacherGradedAtUtc,
            score.FinalizedAtUtc,
            score.Criteria.Select(criterion => new CriterionScoreComparisonResponse(
                criterion.CriterionId,
                criterion.CriterionName,
                criterion.MaxScore,
                criterion.AiScore,
                criterion.AiFeedback,
                criterion.TeacherScore,
                criterion.TeacherFeedback)).ToArray());
}
