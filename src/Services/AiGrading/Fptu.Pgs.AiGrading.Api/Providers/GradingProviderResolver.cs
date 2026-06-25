using Microsoft.Extensions.Options;

namespace Fptu.Pgs.AiGrading.Api.Providers;

public sealed class GradingProviderResolver(
    GeminiGradingProvider gemini,
    MockGradingProvider mock,
    IOptions<AiProviderOptions> options) : IGradingProviderResolver
{
    public IGradingProvider Resolve(string? requestedProvider)
    {
        var provider = string.IsNullOrWhiteSpace(requestedProvider)
            ? options.Value.Provider
            : requestedProvider;

        return provider.ToLowerInvariant() switch
        {
            "gemini" => gemini,
            "mock" => mock,
            _ => throw new InvalidOperationException(
                $"Unsupported AI provider '{provider}'. Supported providers: Gemini, Mock.")
        };
    }
}
