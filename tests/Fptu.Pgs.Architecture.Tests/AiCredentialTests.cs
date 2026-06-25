using Fptu.Pgs.AiGrading.Api.Application;
using Fptu.Pgs.AiGrading.Api.Infrastructure;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class AiCredentialTests
{
    [Fact]
    public async Task Personal_api_key_is_encrypted_and_can_be_deleted()
    {
        var options = new DbContextOptionsBuilder<AiGradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AiGradingDbContext(options);
        var service = new AiCredentialService(
            dbContext,
            new EphemeralDataProtectionProvider());
        var teacherId = Guid.NewGuid();
        const string rawApiKey = "AIzaSyTestKeyThatMustNeverBeStoredAsPlainText";

        var status = await service.SaveAsync(
            teacherId,
            new SaveAiCredentialRequest("Gemini", rawApiKey, true),
            CancellationToken.None);
        var stored = await dbContext.UserAiCredentials.SingleAsync();
        var decrypted = await service.GetDecryptedAsync(
            teacherId,
            "Gemini",
            CancellationToken.None);

        Assert.True(status.HasCredential);
        Assert.DoesNotContain(rawApiKey, stored.ProtectedApiKey);
        Assert.Equal(rawApiKey, decrypted?.ApiKey);
        Assert.True(decrypted?.AllowSystemFallback);

        Assert.True(await service.DeleteAsync(
            teacherId,
            "Gemini",
            CancellationToken.None));
        Assert.Empty(dbContext.UserAiCredentials);
    }

    [Fact]
    public async Task Grading_prefers_personal_key_over_system_key()
    {
        var options = new DbContextOptionsBuilder<AiGradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AiGradingDbContext(options);
        var credentialService = new AiCredentialService(
            dbContext,
            new EphemeralDataProtectionProvider());
        var teacherId = Guid.NewGuid();
        await credentialService.SaveAsync(
            teacherId,
            new SaveAiCredentialRequest(
                "Gemini",
                "AIzaSyPersonalKeyForUnitTestingOnly",
                true),
            CancellationToken.None);

        var service = new GradingExecutionService(
            new FakeProviderResolver(),
            credentialService,
            Options.Create(new AiProviderOptions
            {
                Provider = "Gemini",
                ApiKey = "AIzaSySystemKeyForUnitTestingOnly"
            }),
            NullLogger<GradingExecutionService>.Instance);
        var criterionId = Guid.NewGuid();

        var result = await service.ExecuteAsync(
            new GradeSubmissionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                teacherId,
                "submission text",
                null,
                [new RubricCriterionInput(criterionId, "Criterion", "Description", 10m)],
                "Gemini"),
            CancellationToken.None);

        Assert.Equal("User", result.CredentialSource);
    }

    private sealed class FakeProviderResolver : IGradingProviderResolver
    {
        public IGradingProvider Resolve(string? requestedProvider) => new FakeGeminiProvider();
    }

    private sealed class FakeGeminiProvider : IGradingProvider
    {
        public string Name => "Gemini";

        public Task<ProviderGradingResult> GradeAsync(
            GradeSubmissionRequest request,
            GradingProviderContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProviderGradingResult(
                10m,
                10m,
                "Test",
                1m,
                "Gemini",
                "test-model",
                context.CredentialSource,
                [
                    new ProviderCriterionGrade(
                        request.Criteria.Single().CriterionId,
                        request.Criteria.Single().Name,
                        10m,
                        10m,
                        [],
                        [],
                        "Test",
                        1m)
                ]));
    }
}
