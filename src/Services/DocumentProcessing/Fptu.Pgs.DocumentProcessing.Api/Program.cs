using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();

var app = builder.Build();

if (app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("document-processing-service");

var ocr = app.MapGroup("/ocr").WithTags("OCR and Document Processing");

ocr.MapPost("/jobs", (CreateOcrJobRequest request) =>
{
    var jobId = Guid.NewGuid();

    return Results.Accepted($"/jobs/{jobId}", new
    {
        jobId,
        request.SubmissionId,
        request.Force,
        status = SubmissionStatus.OcrProcessing,
        queuedAtUtc = DateTimeOffset.UtcNow
    });
})
.WithName("CreateOcrJob");

ocr.MapGet("/results/{submissionId:guid}", (Guid submissionId) =>
    Results.Ok(new
    {
        id = Guid.NewGuid(),
        submissionId,
        status = SubmissionStatus.OcrCompleted,
        extractedText = string.Empty,
        textBlocks = Array.Empty<object>(),
        extractedImages = Array.Empty<object>()
    }))
    .WithName("GetOcrResult");

app.Run();

public partial class Program;
