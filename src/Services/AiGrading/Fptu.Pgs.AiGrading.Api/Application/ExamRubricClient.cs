using System.Net;
using System.Net.Http.Json;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.AiGrading.Api.Application;

public sealed class ExamRubricClient(HttpClient httpClient)
{
    public async Task<ExamRubricResponse?> GetAsync(
        Guid examId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"exams/{examId}/rubric",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExamRubricResponse>(
            cancellationToken);
    }
}
