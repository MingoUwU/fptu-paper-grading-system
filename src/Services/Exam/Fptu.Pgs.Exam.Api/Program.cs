using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;
using Fptu.Pgs.Exam.Api.Domain;
using Fptu.Pgs.Exam.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services.AddDbContext<ExamDbContext>(options =>
{
    if (string.Equals(
        builder.Configuration["DatabaseProvider"],
        "InMemory",
        StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase("FptuPgsExam");
        return;
    }

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer =>
        {
            sqlServer.MigrationsHistoryTable("__EFMigrationsHistory", "exam");
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
    await MigrateDatabaseAsync<ExamDbContext>(app);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("exam-rubric-service");

var subjectStore = new List<SubjectResponse>
{
    new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "SWT", "Software Testing", true),
    new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "SRS", "Software Requirement", true),
    new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "PRN232", "Advanced Cross-Platform Application Programming", true)
};
var teacherSubjectAssignments = new List<TeacherSubjectAssignmentResponse>();

var subjects = app.MapGroup("/subjects").WithTags("Subjects");

subjects.MapGet("/", () => Results.Ok(subjectStore))
    .WithName("GetSubjects");

subjects.MapPost("/", (CreateSubjectRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Code) ||
        string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Code and Name are required." });
    }

    if (subjectStore.Any(x =>
        string.Equals(x.Code, request.Code, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict(new { message = "Subject code already exists." });
    }

    var subject = new SubjectResponse(
        Guid.NewGuid(),
        request.Code.Trim().ToUpperInvariant(),
        request.Name.Trim(),
        true);
    subjectStore.Add(subject);

    return Results.Created($"/subjects/{subject.Code}", subject);
})
    .WithName("CreateSubject");

subjects.MapPost(
    "/{subjectCode}/teachers",
    (string subjectCode, AssignTeacherToSubjectRequest request) =>
    {
        var subject = subjectStore.FirstOrDefault(x =>
            string.Equals(x.Code, subjectCode, StringComparison.OrdinalIgnoreCase));
        if (subject is null)
        {
            return Results.NotFound(new { message = "Subject not found." });
        }

        if (!string.Equals(
            subject.Code,
            request.SubjectCode,
            StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new
            {
                message = "Route subjectCode and request SubjectCode must match."
            });
        }

        if (request.TeacherId == Guid.Empty || request.AssignedByAdminId == Guid.Empty)
        {
            return Results.BadRequest(new
            {
                message = "TeacherId and AssignedByAdminId are required."
            });
        }

        var existing = teacherSubjectAssignments.FirstOrDefault(x =>
            x.TeacherId == request.TeacherId &&
            string.Equals(x.SubjectCode, subject.Code, StringComparison.OrdinalIgnoreCase) &&
            x.IsActive);
        if (existing is not null)
        {
            return Results.Ok(existing);
        }

        var assignment = new TeacherSubjectAssignmentResponse(
            Guid.NewGuid(),
            request.TeacherId,
            subject.Code,
            request.AssignedByAdminId,
            DateTimeOffset.UtcNow,
            true);
        teacherSubjectAssignments.Add(assignment);

        return Results.Created(
            $"/subjects/{subject.Code}/teachers/{request.TeacherId}",
            assignment);
    })
    .WithName("AssignTeacherToSubject");

subjects.MapGet(
    "/{subjectCode}/teachers",
    (string subjectCode) =>
    {
        var assignments = teacherSubjectAssignments
            .Where(x => string.Equals(
                x.SubjectCode,
                subjectCode,
                StringComparison.OrdinalIgnoreCase) &&
                x.IsActive)
            .ToArray();

        return Results.Ok(assignments);
    })
    .WithName("GetSubjectTeachers");

app.MapGet(
    "/teachers/{teacherId:guid}/subjects",
    (Guid teacherId) =>
    {
        var assignments = teacherSubjectAssignments
            .Where(x => x.TeacherId == teacherId && x.IsActive)
            .ToArray();

        return Results.Ok(assignments);
    })
    .WithTags("Teacher Subjects")
    .WithName("GetTeacherSubjects");

var exams = app.MapGroup("/exams").WithTags("Exams");

exams.MapGet("/", async (
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var result = await dbContext.Exams
        .AsNoTracking()
        .Include(x => x.Criteria)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new ExamSummaryResponse(
            x.Id,
            x.Code,
            x.Name,
            x.SubjectCode,
            x.Semester,
            x.OriginalFileName,
            x.RubricStatus,
            x.Criteria.Count,
            x.Criteria.Sum(criterion => criterion.MaxScore),
            x.CreatedAtUtc,
            x.PublishedAtUtc))
        .ToListAsync(cancellationToken);

    return Results.Ok(result);
})
    .WithName("ListExams");

exams.MapPost("/import", async (
    HttpRequest httpRequest,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (!httpRequest.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Multipart form data is required." });
    }

    var form = await httpRequest.ReadFormAsync(cancellationToken);
    var code = form["code"].ToString().Trim().ToUpperInvariant();
    var name = form["name"].ToString().Trim();
    var subjectCode = form["subjectCode"].ToString().Trim().ToUpperInvariant();
    var semester = form["semester"].ToString().Trim().ToUpperInvariant();
    var file = form.Files.GetFile("file");

    if (!Guid.TryParse(form["createdByAdminId"], out var createdByAdminId) ||
        createdByAdminId == Guid.Empty ||
        string.IsNullOrWhiteSpace(code) ||
        string.IsNullOrWhiteSpace(name) ||
        string.IsNullOrWhiteSpace(subjectCode) ||
        string.IsNullOrWhiteSpace(semester) ||
        file is null)
    {
        return Results.BadRequest(new
        {
            message = "Code, name, subject, semester, admin and exam file are required."
        });
    }

    var extension = Path.GetExtension(file.FileName);
    if (!extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) &&
        !extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Only DOCX and PDF files are supported." });
    }

    const long maxFileSize = 25L * 1024 * 1024;
    if (file.Length == 0 || file.Length > maxFileSize)
    {
        return Results.BadRequest(new { message = "Exam file must be between 1 byte and 25 MB." });
    }

    if (await dbContext.Exams.AnyAsync(x => x.Code == code, cancellationToken))
    {
        return Results.Conflict(new { message = "Exam code already exists." });
    }

    await using var stream = new MemoryStream();
    await file.CopyToAsync(stream, cancellationToken);
    var now = DateTimeOffset.UtcNow;
    var exam = new ExamDefinition
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        SubjectCode = subjectCode,
        Semester = semester,
        OriginalFileName = Path.GetFileName(file.FileName),
        ContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType,
        DocumentContent = stream.ToArray(),
        RubricStatus = RubricStatus.Draft,
        CreatedByAdminId = createdByAdminId,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    dbContext.Exams.Add(exam);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Created($"/exams/{exam.Id}", ToSummary(exam));
})
    .DisableAntiforgery()
    .WithName("ImportExam");

exams.MapGet("/{examId:guid}/rubric", async (
    Guid examId,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var exam = await dbContext.Exams
        .AsNoTracking()
        .Include(x => x.Criteria)
        .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken);

    return exam is null
        ? Results.NotFound()
        : Results.Ok(ToRubric(exam));
})
    .WithName("GetExamRubric");

exams.MapPut("/{examId:guid}/rubric", async (
    Guid examId,
    SaveExamRubricRequest request,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var validationError = ValidateRubric(request);
    if (validationError is not null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var exam = await dbContext.Exams
        .Include(x => x.Criteria)
        .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken);
    if (exam is null)
    {
        return Results.NotFound();
    }

    var existingIds = exam.Criteria.Select(x => x.Id).ToHashSet();
    if (request.Criteria.Any(x =>
        x.CriterionId.HasValue &&
        x.CriterionId != Guid.Empty &&
        !existingIds.Contains(x.CriterionId.Value)))
    {
        return Results.BadRequest(new
        {
            message = "A criterion id does not belong to this exam."
        });
    }

    var requestedById = request.Criteria
        .Where(x => x.CriterionId.HasValue && x.CriterionId != Guid.Empty)
        .ToDictionary(x => x.CriterionId!.Value);
    foreach (var existingCriterion in exam.Criteria.ToArray())
    {
        if (!requestedById.TryGetValue(existingCriterion.Id, out var requested))
        {
            dbContext.RubricCriteria.Remove(existingCriterion);
            exam.Criteria.Remove(existingCriterion);
            continue;
        }

        ApplyCriterion(existingCriterion, requested);
    }

    foreach (var requested in request.Criteria
        .Where(x => !x.CriterionId.HasValue || x.CriterionId == Guid.Empty)
        .OrderBy(x => x.DisplayOrder))
    {
        var criterion = new RubricCriterion
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id
        };
        ApplyCriterion(criterion, requested);
        exam.Criteria.Add(criterion);
        dbContext.RubricCriteria.Add(criterion);
    }
    exam.RubricStatus = RubricStatus.Draft;
    exam.PublishedAtUtc = null;
    exam.UpdatedAtUtc = DateTimeOffset.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToRubric(exam));
})
    .WithName("SaveExamRubric");

exams.MapPost("/{examId:guid}/rubric/publish", async (
    Guid examId,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var exam = await dbContext.Exams
        .Include(x => x.Criteria)
        .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken);
    if (exam is null)
    {
        return Results.NotFound();
    }

    if (exam.Criteria.Count == 0 || exam.Criteria.Sum(x => x.MaxScore) <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Save at least one valid rubric criterion before publishing."
        });
    }

    exam.RubricStatus = RubricStatus.Published;
    exam.PublishedAtUtc = DateTimeOffset.UtcNow;
    exam.UpdatedAtUtc = DateTimeOffset.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(ToRubric(exam));
})
    .WithName("PublishExamRubric");

exams.MapGet("/{examId:guid}/document", async (
    Guid examId,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var exam = await dbContext.Exams
        .AsNoTracking()
        .Where(x => x.Id == examId)
        .Select(x => new
        {
            x.DocumentContent,
            x.ContentType,
            x.OriginalFileName
        })
        .FirstOrDefaultAsync(cancellationToken);

    return exam is null
        ? Results.NotFound()
        : Results.File(exam.DocumentContent, exam.ContentType, exam.OriginalFileName);
})
    .WithName("DownloadExamDocument");

exams.MapDelete("/{examId:guid}", async (
    Guid examId,
    ExamDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var exam = await dbContext.Exams
        .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken);
    if (exam is null)
    {
        return Results.NotFound();
    }

    dbContext.Exams.Remove(exam);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
    .WithName("DeleteExam");

app.Run();

static string? ValidateRubric(SaveExamRubricRequest request)
{
    if (request.Criteria.Count == 0)
    {
        return "At least one rubric criterion is required.";
    }

    if (request.Criteria.Any(x =>
        string.IsNullOrWhiteSpace(x.Name) ||
        string.IsNullOrWhiteSpace(x.Description) ||
        string.IsNullOrWhiteSpace(x.AiInstructions) ||
        x.MaxScore <= 0 ||
        x.DisplayOrder <= 0))
    {
        return "Every criterion requires a name, description, AI instructions, positive score and display order.";
    }

    if (request.Criteria.Select(x => x.DisplayOrder).Distinct().Count() != request.Criteria.Count)
    {
        return "Display order values must be unique.";
    }

    if (request.Criteria
        .Where(x => x.CriterionId.HasValue && x.CriterionId != Guid.Empty)
        .Select(x => x.CriterionId)
        .Distinct()
        .Count() != request.Criteria.Count(x => x.CriterionId.HasValue && x.CriterionId != Guid.Empty))
    {
        return "Criterion id values must be unique.";
    }

    return null;
}

static void ApplyCriterion(
    RubricCriterion criterion,
    UpsertRubricCriterionRequest request)
{
    criterion.Name = request.Name.Trim();
    criterion.Description = request.Description.Trim();
    criterion.AiInstructions = request.AiInstructions.Trim();
    criterion.MaxScore = request.MaxScore;
    criterion.DisplayOrder = request.DisplayOrder;
}

static ExamSummaryResponse ToSummary(ExamDefinition exam) => new(
    exam.Id,
    exam.Code,
    exam.Name,
    exam.SubjectCode,
    exam.Semester,
    exam.OriginalFileName,
    exam.RubricStatus,
    exam.Criteria.Count,
    exam.Criteria.Sum(x => x.MaxScore),
    exam.CreatedAtUtc,
    exam.PublishedAtUtc);

static ExamRubricResponse ToRubric(ExamDefinition exam) => new(
    exam.Id,
    exam.Code,
    exam.Name,
    exam.SubjectCode,
    exam.Semester,
    exam.OriginalFileName,
    exam.RubricStatus,
    exam.Criteria.Sum(x => x.MaxScore),
    exam.PublishedAtUtc,
    exam.Criteria
        .OrderBy(x => x.DisplayOrder)
        .Select(x => new RubricCriterionResponse(
            x.Id,
            x.Name,
            x.Description,
            x.AiInstructions,
            x.MaxScore,
            x.DisplayOrder))
        .ToArray());

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
