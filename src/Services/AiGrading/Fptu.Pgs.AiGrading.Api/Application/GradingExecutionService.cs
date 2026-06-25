using System.Net;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Application;

public sealed class GradingExecutionService(
    IGradingProviderResolver providerResolver,
    AiCredentialService credentialService,
    ISystemApiKeyPool systemApiKeyPool,
    ILogger<GradingExecutionService> logger)
{
    public async Task<ProviderGradingResult> ExecuteAsync(
        GradeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var provider = providerResolver.Resolve(request.Provider);
        if (!string.Equals(provider.Name, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return await provider.GradeAsync(
                request,
                new GradingProviderContext(null, "None"),
                cancellationToken);
        }

        var userCredential = await credentialService.GetDecryptedAsync(
            request.TeacherId,
            "Gemini",
            cancellationToken);

        if (userCredential is null)
        {
            return await ExecuteWithSystemKeysAsync(
                provider,
                request,
                cancellationToken);
        }

        try
        {
            return await provider.GradeAsync(
                request,
                new GradingProviderContext(userCredential.ApiKey, "User"),
                cancellationToken);
        }
        catch (HttpRequestException exception)
            when (userCredential.AllowSystemFallback &&
                  systemApiKeyPool.Count > 0 &&
                  ShouldFallback(exception.StatusCode))
        {
            logger.LogWarning(
                "User Gemini credential failed for teacher {TeacherId}; retrying with the system key pool.",
                request.TeacherId);

            return await ExecuteWithSystemKeysAsync(
                provider,
                request,
                cancellationToken);
        }
    }

    private async Task<ProviderGradingResult> ExecuteWithSystemKeysAsync(
        IGradingProvider provider,
        GradeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var candidates = systemApiKeyPool.GetCandidates();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                "No system Gemini API key is configured. Set GOOGLE_API_KEY or GOOGLE_API_KEYS.");
        }

        HttpRequestException? lastException = null;
        for (var index = 0; index < candidates.Count; index++)
        {
            try
            {
                return await provider.GradeAsync(
                    request,
                    new GradingProviderContext(candidates[index], "System"),
                    cancellationToken);
            }
            catch (HttpRequestException exception)
                when (ShouldFallback(exception.StatusCode))
            {
                lastException = exception;

                if (index + 1 < candidates.Count)
                {
                    logger.LogWarning(
                        "A system Gemini key failed with status {StatusCode}; trying the next configured key.",
                        exception.StatusCode);
                }
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        throw new InvalidOperationException(
            "All configured system Gemini API keys failed.");
    }

    private static bool ShouldFallback(HttpStatusCode? statusCode) =>
        statusCode is HttpStatusCode.BadRequest or
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
}
