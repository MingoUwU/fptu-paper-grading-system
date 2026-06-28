using Fptu.Pgs.Identity.Api.Application;
using Fptu.Pgs.Identity.Api.Domain;
using Fptu.Pgs.Identity.Api.Infrastructure;
using Fptu.Pgs.BuildingBlocks;
using Fptu.Pgs.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPgsServiceDefaults();
builder.Services.AddScoped<PasswordHashingService>();
builder.Services.AddSingleton<DevelopmentTokenStore>();
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
    DevelopmentTokenStore tokenStore,
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

    var tokens = tokenStore.Issue(user.Id, user.Role);
    var response = new LoginResponse(
        UserId: user.Id,
        Email: user.Email,
        FullName: user.FullName,
        Role: user.Role.ToString(),
        AccessToken: tokens.AccessToken,
        RefreshToken: tokens.RefreshToken,
        ExpiresAtUtc: tokens.ExpiresAtUtc);

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

var users = app.MapGroup("/users").WithTags("User Management");
users.AddEndpointFilter(async (context, next) =>
{
    var tokenStore = context.HttpContext.RequestServices
        .GetRequiredService<DevelopmentTokenStore>();
    var authorization = context.HttpContext.Request.Headers.Authorization.ToString();
    if (!tokenStore.IsAdmin(authorization))
    {
        return Results.Json(
            new { message = "Admin access is required." },
            statusCode: StatusCodes.Status403Forbidden);
    }

    return await next(context);
});

users.MapGet("/", async (
    UserRole? role,
    bool? isActive,
    IdentityDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.Users.AsNoTracking();

    if (role.HasValue)
    {
        query = query.Where(x => x.Role == role.Value);
    }

    if (isActive.HasValue)
    {
        query = query.Where(x => x.IsActive == isActive.Value);
    }

    var result = await query
        .OrderBy(x => x.Role)
        .ThenBy(x => x.FullName)
        .Select(x => new UserAccountResponse(
            x.Id,
            x.Email,
            x.FullName,
            x.Role,
            x.SubjectCode,
            x.IsActive,
            x.CreatedAtUtc,
            x.LastLoginAtUtc))
        .ToListAsync(cancellationToken);

    return Results.Ok(result);
})
.WithName("ListUsers");

users.MapGet("/{userId:guid}", async (
    Guid userId,
    IdentityDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    return user is null
        ? Results.NotFound()
        : Results.Ok(MapUser(user));
})
.WithName("GetUser");

users.MapPost("/", async (
    CreateUserRequest request,
    IdentityDbContext dbContext,
    PasswordHashingService passwordHasher,
    CancellationToken cancellationToken) =>
{
    var validationError = ValidateUserInput(
        request.Email,
        request.FullName,
        request.Password,
        request.Role,
        request.SubjectCode);
    if (validationError is not null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var normalizedEmail = IdentitySeeder.NormalizeEmail(request.Email);
    if (await dbContext.Users.AnyAsync(
        x => x.NormalizedEmail == normalizedEmail,
        cancellationToken))
    {
        return Results.Conflict(new { message = "Email already exists." });
    }

    var password = passwordHasher.HashPassword(request.Password);
    var user = new UserAccount
    {
        Id = Guid.NewGuid(),
        Email = request.Email.Trim(),
        NormalizedEmail = normalizedEmail,
        FullName = request.FullName.Trim(),
        Role = request.Role,
        SubjectCode = NormalizeSubject(request.Role, request.SubjectCode),
        PasswordSalt = password.Salt,
        PasswordHash = password.Hash,
        IsActive = true,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/users/{user.Id}", MapUser(user));
})
.WithName("CreateUser");

users.MapPut("/{userId:guid}", async (
    Guid userId,
    UpdateUserRequest request,
    IdentityDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var validationError = ValidateUserInput(
        "unchanged@fptu.edu.vn",
        request.FullName,
        "unchanged-password",
        request.Role,
        request.SubjectCode);
    if (validationError is not null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var user = await dbContext.Users
        .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NotFound();
    }

    user.FullName = request.FullName.Trim();
    user.Role = request.Role;
    user.SubjectCode = NormalizeSubject(request.Role, request.SubjectCode);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(MapUser(user));
})
.WithName("UpdateUser");

users.MapPatch("/{userId:guid}/status", async (
    Guid userId,
    SetUserStatusRequest request,
    IdentityDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users
        .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NotFound();
    }

    if (!request.IsActive && user.Role == UserRole.Admin)
    {
        var activeAdminCount = await dbContext.Users.CountAsync(
            x => x.Role == UserRole.Admin && x.IsActive,
            cancellationToken);
        if (activeAdminCount <= 1)
        {
            return Results.Conflict(new
            {
                message = "The last active admin account cannot be disabled."
            });
        }
    }

    user.IsActive = request.IsActive;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(MapUser(user));
})
.WithName("SetUserStatus");

users.MapPost("/{userId:guid}/reset-password", async (
    Guid userId,
    ResetUserPasswordRequest request,
    IdentityDbContext dbContext,
    PasswordHashingService passwordHasher,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.NewPassword) ||
        request.NewPassword.Length < 8)
    {
        return Results.BadRequest(new
        {
            message = "Password must contain at least 8 characters."
        });
    }

    var user = await dbContext.Users
        .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NotFound();
    }

    var password = passwordHasher.HashPassword(request.NewPassword);
    user.PasswordSalt = password.Salt;
    user.PasswordHash = password.Hash;
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("ResetUserPassword");

app.Run();

static UserAccountResponse MapUser(UserAccount user) => new(
    user.Id,
    user.Email,
    user.FullName,
    user.Role,
    user.SubjectCode,
    user.IsActive,
    user.CreatedAtUtc,
    user.LastLoginAtUtc);

static string? ValidateUserInput(
    string email,
    string fullName,
    string password,
    UserRole role,
    string? subjectCode)
{
    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
    {
        return "A valid email is required.";
    }

    if (string.IsNullOrWhiteSpace(fullName))
    {
        return "Full name is required.";
    }

    if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
    {
        return "Password must contain at least 8 characters.";
    }

    if (!Enum.IsDefined(role))
    {
        return "Role is invalid.";
    }

    if (role == UserRole.Teacher && string.IsNullOrWhiteSpace(subjectCode))
    {
        return "A teacher must be assigned to a subject.";
    }

    return null;
}

static string? NormalizeSubject(UserRole role, string? subjectCode) =>
    role == UserRole.Teacher
        ? subjectCode?.Trim().ToUpperInvariant()
        : null;

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
