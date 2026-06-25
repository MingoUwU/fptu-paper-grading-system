using System.Net;
using Fptu.Pgs.AiGrading.Api.Providers;
using Fptu.Pgs.Contracts;
using Microsoft.Extensions.Options;

namespace Fptu.Pgs.AiGrading.Api.Application;

public sealed class GradingExecutionService(
    IGradingProviderResolver providerResolver,
    AiCredentialService credentialService,
    IOptions<AiProviderOptions> options,
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
        var systemApiKey = options.Value.ApiKey;

        if (userCredential is null)
        {
            return await provider.GradeAsync(
                request,
                new GradingProviderContext(systemApiKey, "System"),
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
                  !string.IsNullOrWhiteSpace(systemApiKey) &&
                  ShouldFallback(exception.StatusCode))
        {
            logger.LogWarning(
                "User Gemini credential failed for teacher {TeacherId}; retrying with the system credential.",
                request.TeacherId);

            return await provider.GradeAsync(
                request,
                new GradingProviderContext(systemApiKey, "System"),
                cancellationToken);
        }
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
