using Fptu.Pgs.AiGrading.Api.Domain;
using Fptu.Pgs.AiGrading.Api.Infrastructure;
using Fptu.Pgs.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Fptu.Pgs.AiGrading.Api.Application;

public sealed record DecryptedAiCredential(
    string ApiKey,
    bool AllowSystemFallback);

public sealed class AiCredentialService(
    AiGradingDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "Fptu.Pgs.AiGrading.UserApiKey.v1");

    public async Task<AiCredentialStatusResponse> GetStatusAsync(
        Guid teacherId,
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = NormalizeProvider(provider);
        var credential = await dbContext.UserAiCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TeacherId == teacherId &&
                    x.Provider == normalizedProvider &&
                    x.IsActive,
                cancellationToken);

        return credential is null
            ? new AiCredentialStatusResponse(
                teacherId,
                normalizedProvider,
                false,
                null,
                false,
                null,
                null)
            : new AiCredentialStatusResponse(
                teacherId,
                credential.Provider,
                true,
                credential.MaskedApiKey,
                credential.AllowSystemFallback,
                credential.UpdatedAtUtc,
                credential.LastValidatedAtUtc);
    }

    public async Task<AiCredentialStatusResponse> SaveAsync(
        Guid teacherId,
        SaveAiCredentialRequest request,
        CancellationToken cancellationToken)
    {
        if (teacherId == Guid.Empty)
        {
            throw new ArgumentException("TeacherId is required.");
        }

        var provider = NormalizeProvider(request.Provider);
        ValidateApiKey(request.ApiKey);

        var credential = await dbContext.UserAiCredentials
            .FirstOrDefaultAsync(
                x => x.TeacherId == teacherId && x.Provider == provider,
                cancellationToken);

        if (credential is null)
        {
            credential = new UserAiCredential
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                Provider = provider
            };
            dbContext.UserAiCredentials.Add(credential);
        }

        credential.ProtectedApiKey = _protector.Protect(request.ApiKey.Trim());
        credential.MaskedApiKey = MaskApiKey(request.ApiKey);
        credential.AllowSystemFallback = request.AllowSystemFallback;
        credential.IsActive = true;
        credential.UpdatedAtUtc = DateTimeOffset.UtcNow;
        credential.LastValidatedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetStatusAsync(teacherId, provider, cancellationToken);
    }

    public async Task<DecryptedAiCredential?> GetDecryptedAsync(
        Guid teacherId,
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = NormalizeProvider(provider);
        var credential = await dbContext.UserAiCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TeacherId == teacherId &&
                    x.Provider == normalizedProvider &&
                    x.IsActive,
                cancellationToken);

        if (credential is null)
        {
            return null;
        }

        return new DecryptedAiCredential(
            _protector.Unprotect(credential.ProtectedApiKey),
            credential.AllowSystemFallback);
    }

    public async Task MarkValidatedAsync(
        Guid teacherId,
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = NormalizeProvider(provider);
        var credential = await dbContext.UserAiCredentials
            .FirstOrDefaultAsync(
                x => x.TeacherId == teacherId &&
                    x.Provider == normalizedProvider &&
                    x.IsActive,
                cancellationToken);

        if (credential is null)
        {
            return;
        }

        credential.LastValidatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid teacherId,
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = NormalizeProvider(provider);
        var credential = await dbContext.UserAiCredentials
            .FirstOrDefaultAsync(
                x => x.TeacherId == teacherId &&
                    x.Provider == normalizedProvider,
                cancellationToken);

        if (credential is null)
        {
            return false;
        }

        dbContext.UserAiCredentials.Remove(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeProvider(string provider)
    {
        if (!string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only the Gemini BYOK provider is currently supported.");
        }

        return "Gemini";
    }

    private static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Trim().Length < 20)
        {
            throw new ArgumentException("Gemini API key is invalid.");
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        var trimmed = apiKey.Trim();
        var prefixLength = Math.Min(4, trimmed.Length);
        var suffixLength = Math.Min(4, Math.Max(0, trimmed.Length - prefixLength));
        return $"{trimmed[..prefixLength]}••••••••{trimmed[^suffixLength..]}";
    }
}
