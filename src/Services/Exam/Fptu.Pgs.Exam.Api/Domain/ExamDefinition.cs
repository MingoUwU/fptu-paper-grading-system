using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.Exam.Api.Domain;

public sealed class ExamDefinition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] DocumentContent { get; set; } = [];
    public RubricStatus RubricStatus { get; set; } = RubricStatus.Draft;
    public Guid CreatedByAdminId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public List<RubricCriterion> Criteria { get; set; } = [];
}

public sealed class RubricCriterion
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AiInstructions { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public int DisplayOrder { get; set; }
    public ExamDefinition Exam { get; set; } = null!;
}
