using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class SubmissionStatusTests
{
    [Fact]
    public void Status_flow_keeps_teacher_as_final_decision_maker()
    {
        Assert.True(SubmissionStatus.AiSuggested < SubmissionStatus.TeacherReviewed);
        Assert.True(SubmissionStatus.TeacherReviewed < SubmissionStatus.Finalized);
    }
}
