using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("ai-grading-service");

var grading = app.MapGroup("/grading").WithTags("AI Grading");

grading.MapPost("/jobs", (CreateAiGradingJobRequest request) =>
{
    var jobId = Guid.NewGuid();

    return Results.Accepted($"/jobs/{jobId}", new
    {
        jobId,
        request.SubmissionId,
        request.ExamId,
        status = SubmissionStatus.AiGrading,
        queuedAtUtc = DateTimeOffset.UtcNow
    });
})
.WithName("CreateAiGradingJob");

grading.MapGet("/suggestions/{submissionId:guid}", (Guid submissionId) =>
    Results.Ok(new
    {
        id = Guid.NewGuid(),
        submissionId,
        suggestedScore = 0m,
        confidence = 0m,
        criteria = Array.Empty<object>(),
        note = "AI output is a suggestion. A teacher must review and finalize the score."
    }))
    .WithName("GetAiSuggestion");

app.Run();

public partial class Program;
