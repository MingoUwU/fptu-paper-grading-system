using Fptu.Pgs.Contracts;
using Fptu.Pgs.ReviewScore.Api.Domain;

namespace Fptu.Pgs.ReviewScore.Api.Application;

public static class AssignmentMapper
{
    public static GradingAssignmentResponse ToResponse(this GradingAssignment assignment) =>
        new(
            assignment.Id,
            assignment.SubmissionId,
            assignment.ExamId,
            assignment.TeacherId,
            assignment.AssignedByAdminId,
            assignment.Status,
            assignment.AssignedAtUtc,
            assignment.DueAtUtc,
            assignment.StartedAtUtc,
            assignment.TeacherGradedAtUtc,
            assignment.FinalizedAtUtc);

    public static TeacherWorkItemResponse ToWorkItemResponse(
        this GradingAssignment assignment,
        SubmissionScore? score) =>
        new(
            assignment.Id,
            assignment.SubmissionId,
            assignment.ExamId,
            assignment.TeacherId,
            assignment.Status,
            score?.Status,
            score?.AiScore,
            score?.TeacherScore,
            score?.FinalScore,
            assignment.AssignedAtUtc,
            assignment.DueAtUtc);
}
