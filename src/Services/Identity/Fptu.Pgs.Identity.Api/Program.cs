using Fptu.Pgs.Identity.Api.Application;
using Fptu.Pgs.Identity.Api.Infrastructure;
using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services.AddScoped<PasswordHashingService>();
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    if (string.Equals(
        builder.Configuration["DatabaseProvider"],
        "InMemory",
        StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase("FptuPgsIdentity");
        return;
    }

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer =>
        {
            sqlServer.MigrationsHistoryTable(
                "__EFMigrationsHistory",
                "identity");
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

var app = builder.Build();

if (!string.Equals(
    builder.Configuration["DatabaseProvider"],
    "InMemory",
    StringComparison.OrdinalIgnoreCase))
{
    await MigrateDatabaseAsync<IdentityDbContext>(app);
}

await IdentitySeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPgsServiceDefaults("identity-service");

var auth = app.MapGroup("/auth").WithTags("Authentication");

auth.MapPost("/login", async (
    LoginRequest request,
    IdentityDbContext dbContext,
    PasswordHashingService passwordHasher,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email and password are required." });
    }

    var normalizedEmail = IdentitySeeder.NormalizeEmail(request.Email);
    var user = await dbContext.Users
        .FirstOrDefaultAsync(
            x => x.NormalizedEmail == normalizedEmail && x.IsActive,
            cancellationToken);

    if (user is null ||
        !passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordSalt,
            user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    user.LastLoginAtUtc = DateTimeOffset.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    var response = new LoginResponse(
        UserId: user.Id,
        Email: user.Email,
        FullName: user.FullName,
        Role: user.Role.ToString(),
        AccessToken: $"dev-access-{Guid.NewGuid():N}",
        RefreshToken: $"dev-refresh-{Guid.NewGuid():N}",
        ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1));

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

static async Task MigrateDatabaseAsync<TDbContext>(WebApplication app)
    where TDbContext : DbContext
{
    const int maxAttempts = 12;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation(
                "Database migrations applied for {DbContext}.",
                typeof(TDbContext).Name);
            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 3));
            logger.LogWarning(
                exception,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    using var finalScope = app.Services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<TDbContext>();
    await finalDbContext.Database.MigrateAsync();
}

public partial class Program;
