using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("identity-service");

var auth = app.MapGroup("/auth").WithTags("Authentication");

auth.MapPost("/login", (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email and password are required." });
    }

    var response = new LoginResponse(
        AccessToken: $"dev-access-{Guid.NewGuid():N}",
        RefreshToken: $"dev-refresh-{Guid.NewGuid():N}",
        ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
        Role: "Teacher");

    return Results.Ok(response);
})
.WithName("Login");

auth.MapPost("/refresh", (RefreshTokenRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        return Results.BadRequest(new { message = "Refresh token is required." });
    }

    return Results.Ok(new
    {
        accessToken = $"dev-access-{Guid.NewGuid():N}",
        expiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
    });
})
.WithName("RefreshToken");

app.Run();

public partial class Program;
