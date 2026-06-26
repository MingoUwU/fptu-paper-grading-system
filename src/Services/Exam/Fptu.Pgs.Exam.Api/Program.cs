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

exams.MapPost("/", (CreateExamRequest request) =>
{
    var examId = Guid.NewGuid();

    return Results.Created($"/exams/{examId}", new
    {
        id = examId,
        request.Code,
        request.Name,
        request.SubjectCode,
        request.Semester,
        createdAtUtc = DateTimeOffset.UtcNow
    });
})
    .WithName("CreateExam");

exams.MapGet("/{examId:guid}/rubric", (Guid examId) =>
    Results.Ok(new
    {
        examId,
        questions = Array.Empty<object>(),
        totalScore = 10m
    }))
    .WithName("GetExamRubric");

app.MapPost(
    "/questions/{questionId:guid}/rubric-criteria",
    (Guid questionId, AddRubricCriterionRequest request) =>
    {
        var criterionId = Guid.NewGuid();

        return Results.Created(
            $"/questions/{questionId}/rubric-criteria/{criterionId}",
            new
            {
                id = criterionId,
                questionId,
                request.Name,
                request.Description,
                request.MaxScore
            });
    })
    .WithTags("Rubrics")
    .WithName("AddRubricCriterion");

app.Run();

public partial class Program;
