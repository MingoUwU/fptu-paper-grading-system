using Fptu.Pgs.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment()))
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("api-gateway");
app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
    .ExcludeFromDescription();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "FPTU PGS API Documentation";
    options.DisplayRequestDuration();
    options.SwaggerEndpoint("/openapi/identity/v1.json", "Identity Service");
    options.SwaggerEndpoint("/openapi/exam/v1.json", "Exam & Rubric Service");
    options.SwaggerEndpoint("/openapi/submission/v1.json", "Submission Service");
    options.SwaggerEndpoint("/openapi/document-processing/v1.json", "Document Processing Service");
    options.SwaggerEndpoint("/openapi/ai-grading/v1.json", "AI Grading Service");
    options.SwaggerEndpoint("/openapi/review-score/v1.json", "Review Score Service");
    options.SwaggerEndpoint("/openapi/report-audit/v1.json", "Report & Audit Service");
    options.SwaggerEndpoint("/openapi/job-status/v1.json", "Job Status Service");
});
app.MapReverseProxy();

app.Run();

public partial class Program;
