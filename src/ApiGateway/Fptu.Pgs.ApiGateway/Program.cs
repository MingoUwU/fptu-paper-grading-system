using Fptu.Pgs.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("api-gateway");
app.MapReverseProxy();

app.Run();

public partial class Program;
