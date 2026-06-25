using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class SubmissionStatusTests
{
    [Fact]
    public void Status_flow_keeps_teacher_as_final_decision_maker()
    {
        Assert.True(SubmissionStatus.AiGraded < SubmissionStatus.TeacherReviewing);
        Assert.True(SubmissionStatus.TeacherReviewing < SubmissionStatus.TeacherGraded);
        Assert.True(SubmissionStatus.TeacherGraded < SubmissionStatus.Finalized);
    }
}
