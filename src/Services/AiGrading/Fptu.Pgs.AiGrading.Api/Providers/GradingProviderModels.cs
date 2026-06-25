using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Providers;

public sealed record ProviderCriterionGrade(
    Guid CriterionId,
    string CriterionName,
    decimal MaxScore,
    decimal AwardedScore,
    IReadOnlyCollection<string> Evidence,
    IReadOnlyCollection<string> MissingPoints,
    string Feedback,
    decimal Confidence);

public sealed record ProviderGradingResult(
    decimal TotalScore,
    decimal MaxScore,
    string OverallFeedback,
    decimal Confidence,
    string Provider,
    string Model,
    string CredentialSource,
    IReadOnlyCollection<ProviderCriterionGrade> Criteria);

public sealed record GradingProviderContext(
    string? ApiKey,
    string CredentialSource);

public interface IGradingProvider
{
    string Name { get; }

    Task<ProviderGradingResult> GradeAsync(
        GradeSubmissionRequest request,
        GradingProviderContext context,
        CancellationToken cancellationToken);
}

public interface IGradingProviderResolver
{
    IGradingProvider Resolve(string? requestedProvider);
}
