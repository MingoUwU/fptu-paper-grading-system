using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.ReviewScore.Api.Domain;

public sealed class SubmissionScore
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public Guid AiGradingResultId { get; set; }
    public decimal AiScore { get; set; }
    public decimal MaxScore { get; set; }
    public string AiFeedback { get; set; } = string.Empty;
    public decimal AiConfidence { get; set; }
    public string AiProvider { get; set; } = string.Empty;
    public string AiModel { get; set; } = string.Empty;
    public string AiCredentialSource { get; set; } = "None";
    public DateTimeOffset AiGradedAtUtc { get; set; }
    public Guid? TeacherId { get; set; }
    public decimal? TeacherScore { get; set; }
    public string? TeacherFeedback { get; set; }
    public DateTimeOffset? TeacherGradedAtUtc { get; set; }
    public Guid? FinalizedBy { get; set; }
    public decimal? FinalScore { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public SubmissionStatus Status { get; set; }
    public List<CriterionScore> Criteria { get; set; } = [];
    public List<ScoreAuditLog> AuditLogs { get; set; } = [];

    public void ApplyTeacherGrade(SubmitTeacherGradeRequest request)
    {
        if (Status == SubmissionStatus.Finalized)
        {
            throw new ScoreDomainException("A finalized score cannot be changed.");
        }

        if (request.TeacherId == Guid.Empty)
        {
            throw new ScoreDomainException("TeacherId is required.");
        }

        if (request.Score < 0 || request.Score > MaxScore)
        {
            throw new ScoreDomainException(
                $"Teacher score must be between 0 and {MaxScore}.");
        }

        if (request.Criteria.Count != Criteria.Count ||
            request.Criteria.Select(x => x.CriterionId).Distinct().Count() !=
            Criteria.Count)
        {
            throw new ScoreDomainException(
                "Teacher must grade every rubric criterion exactly once.");
        }

        var criteriaById = Criteria.ToDictionary(x => x.CriterionId);
        foreach (var teacherGrade in request.Criteria)
        {
            if (!criteriaById.TryGetValue(teacherGrade.CriterionId, out var criterion))
            {
                throw new ScoreDomainException(
                    $"Unknown criterion '{teacherGrade.CriterionId}'.");
            }

            if (teacherGrade.Score < 0 || teacherGrade.Score > criterion.MaxScore)
            {
                throw new ScoreDomainException(
                    $"Score for '{criterion.CriterionName}' must be between 0 and {criterion.MaxScore}.");
            }
        }

        var criterionTotal = request.Criteria.Sum(x => x.Score);
        if (Math.Abs(criterionTotal - request.Score) > 0.01m)
        {
            throw new ScoreDomainException(
                "Teacher score must equal the sum of criterion scores.");
        }

        var oldScore = TeacherScore ?? AiScore;
        var requestByCriterion = request.Criteria.ToDictionary(x => x.CriterionId);
        foreach (var criterion in Criteria)
        {
            var teacherGrade = requestByCriterion[criterion.CriterionId];
            criterion.TeacherScore = teacherGrade.Score;
            criterion.TeacherFeedback = teacherGrade.Feedback;
        }

        TeacherId = request.TeacherId;
        TeacherScore = request.Score;
        TeacherFeedback = request.Feedback;
        TeacherGradedAtUtc = DateTimeOffset.UtcNow;
        Status = SubmissionStatus.TeacherGraded;
        AuditLogs.Add(new ScoreAuditLog
        {
            Id = Guid.NewGuid(),
            SubmissionScoreId = Id,
            ActorId = request.TeacherId,
            Action = "TeacherGraded",
            OldScore = oldScore,
            NewScore = request.Score,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
    }

    public void FinalizeScore(Guid teacherId)
    {
        if (Status == SubmissionStatus.Finalized)
        {
            return;
        }

        if (teacherId == Guid.Empty)
        {
            throw new ScoreDomainException("TeacherId is required.");
        }

        if (Status != SubmissionStatus.TeacherGraded || !TeacherScore.HasValue)
        {
            throw new ScoreDomainException(
                "Teacher grading is required before finalization.");
        }

        FinalScore = TeacherScore.Value;
        FinalizedBy = teacherId;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
        Status = SubmissionStatus.Finalized;
        AuditLogs.Add(new ScoreAuditLog
        {
            Id = Guid.NewGuid(),
            SubmissionScoreId = Id,
            ActorId = teacherId,
            Action = "ScoreFinalized",
            OldScore = null,
            NewScore = FinalScore,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
    }
}

public sealed class CriterionScore
{
    public Guid Id { get; set; }
    public Guid SubmissionScoreId { get; set; }
    public Guid CriterionId { get; set; }
    public string CriterionName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal AiScore { get; set; }
    public string AiFeedback { get; set; } = string.Empty;
    public decimal? TeacherScore { get; set; }
    public string? TeacherFeedback { get; set; }
    public SubmissionScore SubmissionScore { get; set; } = null!;
}

public sealed class ScoreAuditLog
{
    public Guid Id { get; set; }
    public Guid SubmissionScoreId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal? OldScore { get; set; }
    public decimal? NewScore { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public SubmissionScore SubmissionScore { get; set; } = null!;
}

public sealed class ScoreDomainException(string message) : Exception(message);
