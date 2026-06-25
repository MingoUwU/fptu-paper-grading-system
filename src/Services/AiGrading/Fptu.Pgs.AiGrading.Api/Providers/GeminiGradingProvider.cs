using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fptu.Pgs.Contracts;
using Microsoft.Extensions.Options;

namespace Fptu.Pgs.AiGrading.Api.Providers;

public sealed class GeminiGradingProvider(
    HttpClient httpClient,
    IOptions<AiProviderOptions> options,
    ILogger<GeminiGradingProvider> logger) : IGradingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "Gemini";

    public async Task<ProviderGradingResult> GradeAsync(
        GradeSubmissionRequest request,
        GradingProviderContext context,
        CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        if (string.IsNullOrWhiteSpace(context.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is missing. Configure AiProvider:ApiKey or use the Mock provider.");
        }

        var parts = new List<object>
        {
            new { text = BuildPrompt(request) }
        };

        if (!string.IsNullOrWhiteSpace(request.PdfBase64))
        {
            parts.Add(new
            {
                inlineData = new
                {
                    mimeType = "application/pdf",
                    data = request.PdfBase64
                }
            });
        }

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseJsonSchema = BuildResponseSchema()
            }
        };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(configuration.Model)}:generateContent");
        message.Headers.Add("x-goog-api-key", context.ApiKey);
        message.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Gemini grading failed with status {StatusCode}: {Response}",
                response.StatusCode,
                rawResponse);
            throw new HttpRequestException(
                $"Gemini grading failed with status {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var envelope = JsonSerializer.Deserialize<GeminiResponseEnvelope>(rawResponse, JsonOptions);
        var json = envelope?.Candidates.FirstOrDefault()?
            .Content.Parts.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?
            .Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Gemini returned an empty grading response.");
        }

        var generated = JsonSerializer.Deserialize<GeminiGradeOutput>(json, JsonOptions)
            ?? throw new InvalidOperationException("Gemini returned invalid grading JSON.");

        return NormalizeResult(
            request,
            generated,
            configuration.Model,
            context.CredentialSource);
    }

    public async Task<bool> ValidateApiKeyAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "v1beta/models?pageSize=1");
        message.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static ProviderGradingResult NormalizeResult(
        GradeSubmissionRequest request,
        GeminiGradeOutput generated,
        string model,
        string credentialSource)
    {
        var generatedById = generated.Criteria
            .Where(x => Guid.TryParse(x.CriterionId, out _))
            .GroupBy(x => Guid.Parse(x.CriterionId))
            .ToDictionary(x => x.Key, x => x.First());

        var criteria = request.Criteria.Select(input =>
        {
            generatedById.TryGetValue(input.CriterionId, out var output);
            var awardedScore = Math.Clamp(output?.AwardedScore ?? 0m, 0m, input.MaxScore);

            return new ProviderCriterionGrade(
                input.CriterionId,
                input.Name,
                input.MaxScore,
                decimal.Round(awardedScore, 2),
                output?.Evidence ?? [],
                output?.MissingPoints ?? [],
                output?.Feedback ?? "Gemini did not return feedback for this criterion.",
                Math.Clamp(output?.Confidence ?? 0m, 0m, 1m));
        }).ToArray();

        return new ProviderGradingResult(
            criteria.Sum(x => x.AwardedScore),
            criteria.Sum(x => x.MaxScore),
            generated.OverallFeedback,
            criteria.Length == 0 ? 0m : criteria.Average(x => x.Confidence),
            "Gemini",
            model,
            credentialSource,
            criteria);
    }

    private static string BuildPrompt(GradeSubmissionRequest request)
    {
        var rubricJson = JsonSerializer.Serialize(request.Criteria, JsonOptions);
        var extractedText = string.IsNullOrWhiteSpace(request.ExtractedText)
            ? "(No extracted text was supplied. Inspect the attached PDF.)"
            : request.ExtractedText;

        return $$"""
            You are grading a university Practical Exam submission.

            Rules:
            1. Grade every rubric criterion independently.
            2. Inspect text, tables, screenshots and software diagrams in the PDF when supplied.
            3. Never award more than maxScore.
            4. Evidence must point to concrete content found in the submission.
            5. If evidence is missing or unreadable, award conservatively and explain why.
            6. Return only JSON matching the provided response schema.
            7. The AI score is a first-round grade. A teacher will review and may replace it.

            SubmissionId: {{request.SubmissionId}}
            ExamId: {{request.ExamId}}

            Rubric:
            {{rubricJson}}

            Extracted document text:
            {{extractedText}}
            """;
    }

    private static object BuildResponseSchema() => new
    {
        type = "object",
        properties = new
        {
            overallFeedback = new { type = "string" },
            criteria = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        criterionId = new { type = "string" },
                        awardedScore = new { type = "number" },
                        evidence = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        },
                        missingPoints = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        },
                        feedback = new { type = "string" },
                        confidence = new
                        {
                            type = "number",
                            minimum = 0,
                            maximum = 1
                        }
                    },
                    required = new[]
                    {
                        "criterionId",
                        "awardedScore",
                        "evidence",
                        "missingPoints",
                        "feedback",
                        "confidence"
                    }
                }
            }
        },
        required = new[] { "overallFeedback", "criteria" }
    };

    private sealed record GeminiGradeOutput(
        string OverallFeedback,
        IReadOnlyCollection<GeminiCriterionOutput> Criteria);

    private sealed record GeminiCriterionOutput(
        string CriterionId,
        decimal AwardedScore,
        IReadOnlyCollection<string> Evidence,
        IReadOnlyCollection<string> MissingPoints,
        string Feedback,
        decimal Confidence);

    private sealed record GeminiResponseEnvelope(
        [property: JsonPropertyName("candidates")]
        IReadOnlyCollection<GeminiCandidate> Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")]
        GeminiContent Content);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")]
        IReadOnlyCollection<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")]
        string? Text);
}
