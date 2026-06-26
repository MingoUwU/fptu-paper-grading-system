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

var assignments = app.MapGroup("/assignments").WithTags("Grading Assignments");

assignments.MapPost(
    "/",
    async (
        CreateGradingAssignmentRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var existing = await dbContext.GradingAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubmissionId == request.SubmissionId, cancellationToken);
        if (existing is not null)
        {
            return Results.Conflict(new
            {
                message = "This submission is already assigned.",
                assignment = existing.ToResponse()
            });
        }

        try
        {
            var assignment = GradingAssignment.Create(
                request.SubmissionId,
                request.ExamId,
                request.TeacherId,
                request.AssignedByAdminId,
                request.DueAtUtc);

            dbContext.GradingAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/assignments/{assignment.Id}",
                assignment.ToResponse());
        }
        catch (ScoreDomainException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("CreateGradingAssignment");

assignments.MapPost(
    "/bulk",
    async (
        BulkAssignSubmissionsRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        if (request.SubmissionIds.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one submission is required." });
        }

        var submissionIds = request.SubmissionIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var alreadyAssigned = await dbContext.GradingAssignments
            .Where(x => submissionIds.Contains(x.SubmissionId))
            .Select(x => x.SubmissionId)
            .ToArrayAsync(cancellationToken);
        var alreadyAssignedSet = alreadyAssigned.ToHashSet();

        try
        {
            var assignmentsToCreate = submissionIds
                .Where(x => !alreadyAssignedSet.Contains(x))
                .Select(submissionId => GradingAssignment.Create(
                    submissionId,
                    request.ExamId,
                    request.TeacherId,
                    request.AssignedByAdminId,
                    request.DueAtUtc))
                .ToArray();

            dbContext.GradingAssignments.AddRange(assignmentsToCreate);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                createdCount = assignmentsToCreate.Length,
                skippedAlreadyAssigned = alreadyAssigned,
                assignments = assignmentsToCreate.Select(x => x.ToResponse()).ToArray()
            });
        }
        catch (ScoreDomainException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("BulkAssignSubmissions");

assignments.MapPost(
    "/distribute",
    async (
        AutoDistributeAssignmentsRequest request,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var teacherIds = request.TeacherIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
        var submissionIds = request.SubmissionIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (teacherIds.Length == 0 || submissionIds.Length == 0)
        {
            return Results.BadRequest(new
            {
                message = "At least one teacher and one submission are required."
            });
        }

        var alreadyAssigned = await dbContext.GradingAssignments
            .Where(x => submissionIds.Contains(x.SubmissionId))
            .Select(x => x.SubmissionId)
            .ToArrayAsync(cancellationToken);
        var alreadyAssignedSet = alreadyAssigned.ToHashSet();
        var availableSubmissionIds = submissionIds
            .Where(x => !alreadyAssignedSet.Contains(x))
            .ToArray();

        try
        {
            var assignmentsToCreate = availableSubmissionIds
                .Select((submissionId, index) => GradingAssignment.Create(
                    submissionId,
                    request.ExamId,
                    teacherIds[index % teacherIds.Length],
                    request.AssignedByAdminId,
                    request.DueAtUtc))
                .ToArray();

            dbContext.GradingAssignments.AddRange(assignmentsToCreate);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                createdCount = assignmentsToCreate.Length,
                teacherCount = teacherIds.Length,
                skippedAlreadyAssigned = alreadyAssigned,
                assignments = assignmentsToCreate.Select(x => x.ToResponse()).ToArray()
            });
        }
        catch (ScoreDomainException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("AutoDistributeAssignments");

assignments.MapGet(
    "/teachers/{teacherId:guid}",
    async (
        Guid teacherId,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var teacherAssignments = await dbContext.GradingAssignments
            .AsNoTracking()
            .Where(x => x.TeacherId == teacherId &&
                x.Status != GradingAssignmentStatus.Cancelled)
            .OrderBy(x => x.DueAtUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.AssignedAtUtc)
            .ToArrayAsync(cancellationToken);

        var submissionIds = teacherAssignments
            .Select(x => x.SubmissionId)
            .ToArray();
        var scores = await dbContext.SubmissionScores
            .AsNoTracking()
            .Where(x => submissionIds.Contains(x.SubmissionId))
            .ToDictionaryAsync(x => x.SubmissionId, cancellationToken);

        return Results.Ok(teacherAssignments
            .Select(x =>
            {
                scores.TryGetValue(x.SubmissionId, out var score);
                return x.ToWorkItemResponse(score);
            })
            .ToArray());
    })
    .WithName("GetTeacherWorkload");

assignments.MapGet(
    "/exams/{examId:guid}",
    async (
        Guid examId,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var examAssignments = await dbContext.GradingAssignments
            .AsNoTracking()
            .Where(x => x.ExamId == examId)
            .OrderBy(x => x.TeacherId)
            .ThenBy(x => x.AssignedAtUtc)
            .ToArrayAsync(cancellationToken);

        return Results.Ok(examAssignments.Select(x => x.ToResponse()).ToArray());
    })
    .WithName("GetExamAssignments");

assignments.MapPost(
    "/{assignmentId:guid}/start",
    async (
        Guid assignmentId,
        Guid teacherId,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var assignment = await dbContext.GradingAssignments
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        try
        {
            assignment.MarkInReview(teacherId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(assignment.ToResponse());
        }
        catch (ScoreDomainException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
    })
    .WithName("StartAssignmentReview");

assignments.MapPost(
    "/{assignmentId:guid}/cancel",
    async (
        Guid assignmentId,
        Guid adminId,
        ReviewScoreDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var assignment = await dbContext.GradingAssignments
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        try
        {
            assignment.Cancel(adminId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(assignment.ToResponse());
        }
        catch (ScoreDomainException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
    })
    .WithName("CancelAssignment");

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
            var assignment = await LoadAssignmentBySubmissionAsync(
                dbContext,
                submissionId,
                cancellationToken);
            assignment?.MarkTeacherGraded(request.TeacherId);

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
            var assignment = await LoadAssignmentBySubmissionAsync(
                dbContext,
                submissionId,
                cancellationToken);
            assignment?.MarkFinalized(request.TeacherId);

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

static Task<GradingAssignment?> LoadAssignmentBySubmissionAsync(
    ReviewScoreDbContext dbContext,
    Guid submissionId,
    CancellationToken cancellationToken) =>
    dbContext.GradingAssignments
        .FirstOrDefaultAsync(
            x => x.SubmissionId == submissionId &&
                x.Status != GradingAssignmentStatus.Cancelled,
            cancellationToken);

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
