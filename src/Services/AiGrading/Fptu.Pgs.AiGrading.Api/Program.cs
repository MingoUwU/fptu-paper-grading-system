using Fptu.Pgs.AiGrading.Api.Application;
using Fptu.Pgs.AiGrading.Api.Infrastructure;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services.AddDbContext<AiGradingDbContext>(options =>
{
    if (string.Equals(
        builder.Configuration["DatabaseProvider"],
        "InMemory",
        StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase("FptuPgsAiGrading");
        return;
    }

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer => sqlServer.MigrationsHistoryTable(
            "__EFMigrationsHistory",
            "grading"));
});
builder.Services.Configure<AiProviderOptions>(
    builder.Configuration.GetSection(AiProviderOptions.SectionName));
builder.Services.PostConfigure<AiProviderOptions>(options =>
{
    var explicitProvider = builder.Configuration["AI_PROVIDER"];
    if (!string.IsNullOrWhiteSpace(explicitProvider))
    {
        options.Provider = explicitProvider;
        return;
    }

    if (!string.IsNullOrWhiteSpace(builder.Configuration["GOOGLE_API_KEY"]) ||
        !string.IsNullOrWhiteSpace(builder.Configuration["GOOGLE_API_KEYS"]))
    {
        options.Provider = "Gemini";
    }
});
var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("Fptu.Pgs.AiGrading");
if (!string.IsNullOrWhiteSpace(keyRingPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

builder.Services.AddSingleton<MockGradingProvider>();
builder.Services.AddHttpClient<GeminiGradingProvider>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<AiProviderOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<IGradingProviderResolver, GradingProviderResolver>();
builder.Services.AddScoped<AiCredentialService>();
builder.Services.AddSingleton<ISystemApiKeyPool, SystemApiKeyPool>();
builder.Services.AddScoped<GradingExecutionService>();
builder.Services.AddHttpClient<ReviewScoreClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ReviewScoreBaseUrl"]
        ?? "http://localhost:5106/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("ai-grading-service");

var grading = app.MapGroup("/grading").WithTags("AI Grading");

grading.MapPost(
    "/evaluate",
    async (
        GradeSubmissionRequest request,
        GradingExecutionService gradingExecutionService,
        AiGradingDbContext dbContext,
        ReviewScoreClient reviewScoreClient,
        CancellationToken cancellationToken) =>
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return Results.BadRequest(new { message = validationError });
        }

        try
        {
            var providerResult = await gradingExecutionService.ExecuteAsync(
                request,
                cancellationToken);
            var result = AiGradingMapper.ToEntity(request, providerResult);

            dbContext.AiGradingResults.Add(result);
            await dbContext.SaveChangesAsync(cancellationToken);

            result.ReviewScoreSynchronized = await reviewScoreClient.TryRegisterAsync(
                result.ToRegisterRequest(),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/grading/results/{result.SubmissionId}",
                result.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("EvaluateSubmission");

grading.MapGet(
    "/credentials/{teacherId:guid}",
    async (
        Guid teacherId,
        string? provider,
        AiCredentialService credentialService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            return Results.Ok(await credentialService.GetStatusAsync(
                teacherId,
                provider ?? "Gemini",
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("GetAiCredentialStatus");

grading.MapPut(
    "/credentials/{teacherId:guid}",
    async (
        Guid teacherId,
        SaveAiCredentialRequest request,
        AiCredentialService credentialService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var status = await credentialService.SaveAsync(
                teacherId,
                request,
                cancellationToken);
            return Results.Ok(status);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("SaveAiCredential");

grading.MapPost(
    "/credentials/{teacherId:guid}/test",
    async (
        Guid teacherId,
        string? provider,
        AiCredentialService credentialService,
        GeminiGradingProvider geminiProvider,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var providerName = provider ?? "Gemini";
            var credential = await credentialService.GetDecryptedAsync(
                teacherId,
                providerName,
                cancellationToken);
            if (credential is null)
            {
                return Results.NotFound(new { message = "No personal API key is configured." });
            }

            var isValid = await geminiProvider.ValidateApiKeyAsync(
                credential.ApiKey,
                cancellationToken);
            if (isValid)
            {
                await credentialService.MarkValidatedAsync(
                    teacherId,
                    providerName,
                    cancellationToken);
            }

            return Results.Ok(new AiCredentialValidationResponse(
                isValid,
                providerName,
                isValid ? "API key is valid." : "API key was rejected by Gemini.",
                DateTimeOffset.UtcNow));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("TestAiCredential");

grading.MapDelete(
    "/credentials/{teacherId:guid}",
    async (
        Guid teacherId,
        string? provider,
        AiCredentialService credentialService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var deleted = await credentialService.DeleteAsync(
                teacherId,
                provider ?? "Gemini",
                cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    })
    .WithName("DeleteAiCredential");

grading.MapGet(
    "/results/{submissionId:guid}",
    async (
        Guid submissionId,
        AiGradingDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var result = await dbContext.AiGradingResults
            .AsNoTracking()
            .Include(x => x.Criteria)
            .Where(x => x.SubmissionId == submissionId)
            .OrderByDescending(x => x.GradedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result.ToResponse());
    })
    .WithName("GetAiGradingResult");

grading.MapGet(
    "/suggestions/{submissionId:guid}",
    async (
        Guid submissionId,
        AiGradingDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var result = await dbContext.AiGradingResults
            .AsNoTracking()
            .Include(x => x.Criteria)
            .Where(x => x.SubmissionId == submissionId)
            .OrderByDescending(x => x.GradedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result.ToResponse());
    })
    .WithName("GetAiSuggestionCompatibility");

app.Run();

static string? ValidateRequest(GradeSubmissionRequest request)
{
    if (request.SubmissionId == Guid.Empty ||
        request.ExamId == Guid.Empty ||
        request.TeacherId == Guid.Empty)
    {
        return "SubmissionId, ExamId and TeacherId are required.";
    }

    if (request.Criteria.Count == 0)
    {
        return "At least one rubric criterion is required.";
    }

    if (request.Criteria.Any(x =>
        x.CriterionId == Guid.Empty ||
        string.IsNullOrWhiteSpace(x.Name) ||
        x.MaxScore <= 0))
    {
        return "Every criterion requires an id, name and positive MaxScore.";
    }

    if (request.Criteria.Select(x => x.CriterionId).Distinct().Count() != request.Criteria.Count)
    {
        return "CriterionId values must be unique.";
    }

    if (string.IsNullOrWhiteSpace(request.ExtractedText) &&
        string.IsNullOrWhiteSpace(request.PdfBase64))
    {
        return "ExtractedText or PdfBase64 is required.";
    }

    return null;
}

public partial class Program;
