namespace Fptu.Pgs.AiGrading.Api.Providers;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string Provider { get; set; } = "Mock";
    public string Model { get; set; } = "gemini-3.5-flash";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeys { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";
}
