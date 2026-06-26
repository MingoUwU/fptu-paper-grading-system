using Fptu.Pgs.Contracts;
using Fptu.Pgs.ReviewScore.Api.Domain;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class GradingAssignmentTests
{
    [Fact]
    public void Assignment_allows_only_the_assigned_teacher_to_grade()
    {
        var assignedTeacherId = Guid.NewGuid();
        var otherTeacherId = Guid.NewGuid();
        var assignment = GradingAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedTeacherId,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(7));

        var exception = Assert.Throws<ScoreDomainException>(
            () => assignment.MarkTeacherGraded(otherTeacherId));

        Assert.Contains("assigned to another teacher", exception.Message);
        assignment.MarkTeacherGraded(assignedTeacherId);
        Assert.Equal(GradingAssignmentStatus.TeacherGraded, assignment.Status);
        Assert.NotNull(assignment.TeacherGradedAtUtc);
    }

    [Fact]
    public void Finalized_assignment_cannot_be_cancelled()
    {
        var teacherId = Guid.NewGuid();
        var assignment = GradingAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            teacherId,
            Guid.NewGuid(),
            null);

        assignment.MarkTeacherGraded(teacherId);
        assignment.MarkFinalized(teacherId);

        var exception = Assert.Throws<ScoreDomainException>(
            () => assignment.Cancel(Guid.NewGuid()));

        Assert.Contains("finalized assignment cannot be cancelled", exception.Message);
    }
}
