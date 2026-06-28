using System.Collections.Concurrent;
using Fptu.Pgs.Contracts;

namespace Fptu.Pgs.Identity.Api.Application;

public sealed class DevelopmentTokenStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = [];

    public IssuedTokens Issue(Guid userId, UserRole role)
    {
        var accessToken = $"dev-access-{Guid.NewGuid():N}";
        var refreshToken = $"dev-refresh-{Guid.NewGuid():N}";
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(1);
        _sessions[accessToken] = new Session(userId, role, expiresAtUtc);
        return new IssuedTokens(accessToken, refreshToken, expiresAtUtc);
    }

    public bool IsAdmin(string? authorizationHeader)
    {
        const string bearerPrefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(
                bearerPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorizationHeader[bearerPrefix.Length..].Trim();
        if (!_sessions.TryGetValue(token, out var session))
        {
            return false;
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        return session.Role == UserRole.Admin;
    }

    public sealed record IssuedTokens(
        string AccessToken,
        string RefreshToken,
        DateTimeOffset ExpiresAtUtc);

    private sealed record Session(
        Guid UserId,
        UserRole Role,
        DateTimeOffset ExpiresAtUtc);
}
