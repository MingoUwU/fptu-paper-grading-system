using Fptu.Pgs.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();

var app = builder.Build();

if (app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("report-audit-service");

app.MapGet("/reports/export", (Guid batchId, string? format) =>
{
    var normalizedFormat = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
        ? "pdf"
        : "xlsx";
    var exportId = Guid.NewGuid();

    return Results.Accepted($"/reports/exports/{exportId}", new
    {
        exportId,
        batchId,
        format = normalizedFormat,
        status = "Queued",
        queuedAtUtc = DateTimeOffset.UtcNow
    });
})
.WithTags("Reports")
.WithName("ExportReport");

app.MapGet("/audit-logs", (Guid? submissionId, int take = 50) =>
    Results.Ok(new
    {
        submissionId,
        take = Math.Clamp(take, 1, 200),
        items = Array.Empty<object>()
    }))
    .WithTags("Audit")
    .WithName("GetAuditLogs");

app.Run();

public partial class Program;
