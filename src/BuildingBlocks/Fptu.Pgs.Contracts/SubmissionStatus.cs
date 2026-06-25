namespace Fptu.Pgs.Contracts;

public enum SubmissionStatus
{
    Uploaded = 1,
    OcrProcessing = 2,
    OcrCompleted = 3,
    AiGrading = 4,
    AiSuggested = 5,
    TeacherReviewed = 6,
    Finalized = 7,
    Failed = 8
}
