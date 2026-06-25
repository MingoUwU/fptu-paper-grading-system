using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fptu.Pgs.BuildingBlocks;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddPgsServiceDefaults(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });
        services.AddHealthChecks();

        return services;
    }

    public static WebApplication MapPgsServiceDefaults(
        this WebApplication app,
        string serviceName)
    {
        app.UseExceptionHandler();
        app.MapHealthChecks("/health", new HealthCheckOptions());
        app.MapGet("/", () => Results.Ok(new
        {
            service = serviceName,
            status = "running",
            utc = DateTimeOffset.UtcNow
        }))
        .ExcludeFromDescription();

        return app;
    }
}
