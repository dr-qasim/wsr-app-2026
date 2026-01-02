using System.Security.Cryptography;
using System.Text;

namespace VendingService.API.Auth;

public sealed class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int Iterations = 100_000;

    public (string PasswordHash, string PasswordSalt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hashBytes = Pbkdf2(password, saltBytes, Iterations, KeySizeBytes);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string passwordHash, string? passwordSalt)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
        {
            return false;
        }

        if (!TryFromBase64(passwordHash, out var expectedHashBytes))
        {
            return false;
        }

        if (!TryFromBase64(passwordSalt, out var saltBytes))
        {
            return false;
        }

        var actualHashBytes = Pbkdf2(password, saltBytes, Iterations, expectedHashBytes.Length);
        return CryptographicOperations.FixedTimeEquals(expectedHashBytes, actualHashBytes);
    }

    private static byte[] Pbkdf2(string password, byte[] saltBytes, int iterations, int keySizeBytes)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            iterations,
            HashAlgorithmName.SHA256,
            keySizeBytes);
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
