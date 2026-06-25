namespace Fptu.Pgs.Contracts;

public enum SubmissionStatus
{
    Uploaded = 1,
    OcrProcessing = 2,
    OcrCompleted = 3,
    AiGrading = 4,
    AiGraded = 5,
    TeacherReviewing = 6,
    TeacherGraded = 7,
    Finalized = 8,
    Failed = 9
}
