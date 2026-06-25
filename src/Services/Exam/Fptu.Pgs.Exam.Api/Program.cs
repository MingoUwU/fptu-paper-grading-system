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
