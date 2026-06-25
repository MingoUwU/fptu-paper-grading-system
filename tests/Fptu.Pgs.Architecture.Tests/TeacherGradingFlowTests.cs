using Fptu.Pgs.Contracts;
using Fptu.Pgs.ReviewScore.Api.Application;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class TeacherGradingFlowTests
{
    [Fact]
    public void Teacher_score_becomes_final_without_overwriting_ai_score()
    {
        var criterionId = Guid.NewGuid();
        var score = ScoreMapper.FromAiGrade(new RegisterAiGradeRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            7.5m,
            10m,
            "AI feedback",
            0.82m,
            "Gemini",
            "gemini-test",
            "User",
            DateTimeOffset.UtcNow,
            [
                new AiCriterionGradeResponse(
                    criterionId,
                    "Software diagram",
                    10m,
                    7.5m,
                    ["Diagram contains the required actor."],
                    ["Missing alternate flow."],
                    "Add the alternate flow.",
                    0.82m)
            ]));

        var teacherId = Guid.NewGuid();
        score.ApplyTeacherGrade(new SubmitTeacherGradeRequest(
            teacherId,
            8m,
            "Teacher reviewed the diagram.",
            [
                new TeacherCriterionGradeRequest(
                    criterionId,
                    8m,
                    "Accepted after manual review.")
            ]));
        score.FinalizeScore(teacherId);

        Assert.Equal(7.5m, score.AiScore);
        Assert.Equal(8m, score.TeacherScore);
        Assert.Equal(8m, score.FinalScore);
        Assert.Equal(SubmissionStatus.Finalized, score.Status);
        Assert.Equal(2, score.AuditLogs.Count);
    }

    [Fact]
    public void Finalize_requires_teacher_grading()
    {
        var score = ScoreMapper.FromAiGrade(new RegisterAiGradeRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5m,
            10m,
            "AI feedback",
            0.5m,
            "Mock",
            "mock-v1",
            "None",
            DateTimeOffset.UtcNow,
            []));

        var exception = Assert.Throws<Fptu.Pgs.ReviewScore.Api.Domain.ScoreDomainException>(
            () => score.FinalizeScore(Guid.NewGuid()));

        Assert.Contains("Teacher grading is required", exception.Message);
    }
}
