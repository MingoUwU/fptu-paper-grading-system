using System.Net.Http.Json;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Application;

public sealed class ReviewScoreClient(
    HttpClient httpClient,
    ILogger<ReviewScoreClient> logger)
{
    public async Task<bool> TryRegisterAsync(
        RegisterAiGradeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "scores/ai-grade",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return true;
            }

            logger.LogWarning(
                "Review Score synchronization failed with status {StatusCode}.",
                response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Review Score service is unavailable. The AI result remains stored for retry.");
        }

        return false;
    }
}
