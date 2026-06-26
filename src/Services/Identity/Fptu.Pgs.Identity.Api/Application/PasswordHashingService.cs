using System.Security.Cryptography;

namespace Fptu.Pgs.Identity.Api.Application;

public sealed class PasswordHashingService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (byte[] Salt, byte[] Hash) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        return (salt, ComputeHash(password, salt));
    }

    public bool VerifyPassword(string password, byte[] salt, byte[] expectedHash)
    {
        var actualHash = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);
}
