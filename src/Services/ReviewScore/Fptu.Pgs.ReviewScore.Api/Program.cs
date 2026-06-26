using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;
using Fptu.Pgs.ReviewScore.Api.Application;
using Fptu.Pgs.ReviewScore.Api.Domain;
using Fptu.Pgs.ReviewScore.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services.AddDbContext<ReviewScoreDbContext>(options =>
{
    if (string.Equals(
        builder.Configuration["DatabaseProvider"],
        "InMemory",
        StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase("FptuPgsReviewScore");
        return;
    }

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer =>
        {
            sqlServer.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                "score");
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

var app = builder.Build();

if (!string.Equals(
    builder.Configuration["DatabaseProvider"],
    "InMemory",
    StringComparison.OrdinalIgnoreCase))
{
    await MigrateDatabaseAsync<ReviewScoreDbContext>(app);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("review-score-service");

var scores = app.MapGroup("/scores").WithTags("Review and Scores");

scores.MapPost(
    "/ai-grade",
    async (
        RegisterAiGradeRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var existing = await dbContext.SubmissionScores
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SubmissionId == request.SubmissionId ||
                    x.AiGradingResultId == request.AiGradingResultId,
                cancellationToken);

        if (existing is not null)
        {
            return existing.AiGradingResultId == request.AiGradingResultId
                ? Results.Ok(new { existing.Id, existing.Status, idempotent = true })
                : Results.Conflict(new
                {
                    message = "This submission already has a different AI grading result."
                });
        }

        if (request.AiScore < 0 ||
            request.AiScore > request.MaxScore ||
            request.Criteria.Sum(x => x.AwardedScore) != request.AiScore)
        {
            return Results.BadRequest(new { message = "Invalid AI score totals." });
        }

        var score = ScoreMapper.FromAiGrade(request);
        dbContext.SubmissionScores.Add(score);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/scores/submissions/{score.SubmissionId}",
            score.ToResponse());
    })
    .WithName("RegisterAiGrade");

scores.MapGet(
    "/submissions/{submissionId:guid}",
    async (
        Guid submissionId,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var score = await LoadScoreAsync(dbContext, submissionId, cancellationToken);

        return score is null
            ? Results.NotFound()
            : Results.Ok(score.ToResponse());
    })
    .WithName("GetScoreComparison");

scores.MapPut(
    "/submissions/{submissionId:guid}/teacher-grade",
    async (
        Guid submissionId,
        SubmitTeacherGradeRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var score = await LoadScoreAsync(dbContext, submissionId, cancellationToken);
        if (score is null)
        {
            return Results.NotFound();
        }

        try
        {
            score.ApplyTeacherGrade(request);
        }
        catch (ScoreDomainException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }

        dbContext.ScoreAuditLogs.Add(score.AuditLogs[^1]);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(score.ToResponse());
    })
    .WithName("SubmitTeacherGrade");

scores.MapPost(
    "/submissions/{submissionId:guid}/finalize",
    async (
        Guid submissionId,
        FinalizeScoreRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var score = await LoadScoreAsync(dbContext, submissionId, cancellationToken);
        if (score is null)
        {
            return Results.NotFound();
        }

        var auditCount = score.AuditLogs.Count;
        try
        {
            score.FinalizeScore(request.TeacherId);
        }
        catch (ScoreDomainException exception)
        {
            return Results.Conflict(new
            {
                message = exception.Message
            });
        }

        if (score.AuditLogs.Count > auditCount)
        {
            dbContext.ScoreAuditLogs.Add(score.AuditLogs[^1]);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(score.ToResponse());
    })
    .WithName("FinalizeScore");

app.Run();

static Task<SubmissionScore?> LoadScoreAsync(
    ReviewScoreDbContext dbContext,
    Guid submissionId,
    CancellationToken cancellationToken) =>
    dbContext.SubmissionScores
        .Include(x => x.Criteria)
        .Include(x => x.AuditLogs)
        .FirstOrDefaultAsync(x => x.SubmissionId == submissionId, cancellationToken);

static async Task MigrateDatabaseAsync<TDbContext>(WebApplication app)
    where TDbContext : DbContext
{
    const int maxAttempts = 12;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation(
                "Database migrations applied for {DbContext}.",
                typeof(TDbContext).Name);
            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 3));
            logger.LogWarning(
                exception,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    using var finalScope = app.Services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<TDbContext>();
    await finalDbContext.Database.MigrateAsync();
}

public partial class Program;
