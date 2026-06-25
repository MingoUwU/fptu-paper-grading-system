using Fptu.Pgs.AiGrading.Api.Application;
using Fptu.Pgs.AiGrading.Api.Infrastructure;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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
            new FakeSystemApiKeyPool("AIzaSySystemKeyForUnitTestingOnly"),
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

    [Fact]
    public void System_key_pool_reads_primary_and_comma_separated_keys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GOOGLE_API_KEY"] = "primary-key",
                ["GOOGLE_API_KEYS"] = "second-key, third-key,primary-key"
            })
            .Build();
        var pool = new SystemApiKeyPool(configuration);

        var firstRequest = pool.GetCandidates();
        var secondRequest = pool.GetCandidates();

        Assert.Equal(3, pool.Count);
        Assert.Equal(
            ["primary-key", "second-key", "third-key"],
            firstRequest);
        Assert.Equal(
            ["second-key", "third-key", "primary-key"],
            secondRequest);
    }

    [Fact]
    public async Task System_key_pool_retries_the_next_key_after_quota_error()
    {
        var options = new DbContextOptionsBuilder<AiGradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AiGradingDbContext(options);
        var credentialService = new AiCredentialService(
            dbContext,
            new EphemeralDataProtectionProvider());
        var provider = new RetryingFakeGeminiProvider("quota-key");
        var service = new GradingExecutionService(
            new SingleProviderResolver(provider),
            credentialService,
            new FakeSystemApiKeyPool("quota-key", "working-key"),
            NullLogger<GradingExecutionService>.Instance);

        var result = await service.ExecuteAsync(
            CreateGradeRequest(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(["quota-key", "working-key"], provider.UsedKeys);
        Assert.Equal("System", result.CredentialSource);
    }

    [Fact]
    public async Task One_personal_key_still_works_without_using_the_system_pool()
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
                "AIzaSyOnlyPersonalKeyForUnitTesting",
                false),
            CancellationToken.None);
        var provider = new RetryingFakeGeminiProvider();
        var service = new GradingExecutionService(
            new SingleProviderResolver(provider),
            credentialService,
            new FakeSystemApiKeyPool("unused-system-key"),
            NullLogger<GradingExecutionService>.Instance);

        var result = await service.ExecuteAsync(
            CreateGradeRequest(teacherId),
            CancellationToken.None);

        Assert.Equal(["AIzaSyOnlyPersonalKeyForUnitTesting"], provider.UsedKeys);
        Assert.Equal("User", result.CredentialSource);
    }

    private static GradeSubmissionRequest CreateGradeRequest(Guid teacherId)
    {
        var criterionId = Guid.NewGuid();
        return new GradeSubmissionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            teacherId,
            "submission text",
            null,
            [new RubricCriterionInput(criterionId, "Criterion", "Description", 10m)],
            "Gemini");
    }

    private sealed class FakeProviderResolver : IGradingProviderResolver
    {
        public IGradingProvider Resolve(string? requestedProvider) => new FakeGeminiProvider();
    }

    private sealed class SingleProviderResolver(IGradingProvider provider)
        : IGradingProviderResolver
    {
        public IGradingProvider Resolve(string? requestedProvider) => provider;
    }

    private sealed class FakeSystemApiKeyPool(params string[] keys) : ISystemApiKeyPool
    {
        public int Count => keys.Length;

        public IReadOnlyList<string> GetCandidates() => keys;
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

    private sealed class RetryingFakeGeminiProvider(params string[] failingKeys)
        : IGradingProvider
    {
        public string Name => "Gemini";
        public List<string> UsedKeys { get; } = [];

        public Task<ProviderGradingResult> GradeAsync(
            GradeSubmissionRequest request,
            GradingProviderContext context,
            CancellationToken cancellationToken)
        {
            UsedKeys.Add(context.ApiKey!);
            if (failingKeys.Contains(context.ApiKey, StringComparer.Ordinal))
            {
                throw new HttpRequestException(
                    "Quota exhausted.",
                    null,
                    System.Net.HttpStatusCode.TooManyRequests);
            }

            return Task.FromResult(new ProviderGradingResult(
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
}
