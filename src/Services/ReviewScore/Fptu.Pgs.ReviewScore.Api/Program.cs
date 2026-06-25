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

app.MapPgsServiceDefaults("review-score-service");

var scores = app.MapGroup("/scores").WithTags("Review and Scores");

scores.MapPut("/{scoreId:guid}", (Guid scoreId, UpdateScoreRequest request) =>
    Results.Ok(new
    {
        scoreId,
        request.Score,
        request.Feedback,
        status = SubmissionStatus.TeacherReviewed,
        updatedAtUtc = DateTimeOffset.UtcNow
    }))
    .WithName("UpdateScore");

scores.MapPost("/finalize", (FinalizeScoreRequest request) =>
{
    var finalScoreId = Guid.NewGuid();

    return Results.Ok(new
    {
        finalScoreId,
        request.SubmissionId,
        request.Score,
        request.Feedback,
        status = SubmissionStatus.Finalized,
        finalizedAtUtc = DateTimeOffset.UtcNow
    });
})
.WithName("FinalizeScore");

app.Run();

public partial class Program;
