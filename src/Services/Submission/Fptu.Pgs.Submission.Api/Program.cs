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

app.MapPgsServiceDefaults("submission-service");

var batches = app.MapGroup("/batches").WithTags("Submission Batches");

batches.MapPost("/upload", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new
        {
            message = "Use multipart/form-data and attach one or more files."
        });
    }

    var form = await request.ReadFormAsync(cancellationToken);
    if (form.Files.Count == 0)
    {
        return Results.BadRequest(new { message = "At least one file is required." });
    }

    var response = new BatchUploadResponse(
        BatchId: Guid.NewGuid(),
        FileCount: form.Files.Count,
        Status: SubmissionStatus.Uploaded);

    return Results.Accepted($"/batches/{response.BatchId}", response);
})
.DisableAntiforgery()
.WithName("UploadBatch");

batches.MapGet("/{batchId:guid}", (Guid batchId) =>
    Results.Ok(new
    {
        batchId,
        status = SubmissionStatus.OcrProcessing,
        submissions = Array.Empty<object>(),
        createdAtUtc = DateTimeOffset.UtcNow
    }))
    .WithName("GetBatch");

app.Run();

public partial class Program;
