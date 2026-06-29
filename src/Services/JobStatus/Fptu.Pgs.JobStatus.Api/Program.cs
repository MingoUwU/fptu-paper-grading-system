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

app.MapPgsServiceDefaults("job-status-service");

app.MapGet("/jobs/{jobId:guid}", (Guid jobId, Guid? submissionId) =>
    Results.Ok(new JobStatusResponse(
        JobId: jobId,
        SubmissionId: submissionId ?? Guid.Empty,
        Status: SubmissionStatus.OcrProcessing,
        ProgressPercent: 25,
        Error: null)))
    .WithTags("Jobs")
    .WithName("GetJobStatus");

app.MapGet("/batches/{batchId:guid}/progress", (Guid batchId) =>
    Results.Ok(new
    {
        batchId,
        total = 0,
        completed = 0,
        failed = 0,
        progressPercent = 0,
        submissions = Array.Empty<JobStatusResponse>()
    }))
    .WithTags("Jobs")
    .WithName("GetBatchProgress");

app.Run();

public partial class Program;
