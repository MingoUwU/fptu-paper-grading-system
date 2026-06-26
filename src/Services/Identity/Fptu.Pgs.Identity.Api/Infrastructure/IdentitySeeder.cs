using Fptu.Pgs.Contracts;
using Fptu.Pgs.Identity.Api.Application;
using Fptu.Pgs.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.Identity.Api.Infrastructure;

public static class IdentitySeeder
{
    public static readonly Guid AdminId =
        Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    public static readonly Guid TeacherSwtId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TeacherSrsId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHashingService>();

        await UpsertUserAsync(
            dbContext,
            passwordHasher,
            AdminId,
            "admin@fptu.edu.vn",
            "FPTU PGS Admin",
            UserRole.Admin,
            null,
            "Admin@123");
        await UpsertUserAsync(
            dbContext,
            passwordHasher,
            TeacherSwtId,
            "teacher.swt@fptu.edu.vn",
            "Teacher Software Testing",
            UserRole.Teacher,
            "SWT",
            "Teacher@123");
        await UpsertUserAsync(
            dbContext,
            passwordHasher,
            TeacherSrsId,
            "teacher.srs@fptu.edu.vn",
            "Teacher Software Requirement",
            UserRole.Teacher,
            "SRS",
            "Teacher@123");

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpsertUserAsync(
        IdentityDbContext dbContext,
        PasswordHashingService passwordHasher,
        Guid id,
        string email,
        string fullName,
        UserRole role,
        string? subjectCode,
        string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id || x.NormalizedEmail == normalizedEmail);

        if (user is null)
        {
            var hash = passwordHasher.HashPassword(password);
            dbContext.Users.Add(new UserAccount
            {
                Id = id,
                Email = email,
                NormalizedEmail = normalizedEmail,
                FullName = fullName,
                Role = role,
                SubjectCode = subjectCode,
                PasswordSalt = hash.Salt,
                PasswordHash = hash.Hash,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            return;
        }

        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.FullName = fullName;
        user.Role = role;
        user.SubjectCode = subjectCode;
        user.IsActive = true;
    }

    public static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();
}
