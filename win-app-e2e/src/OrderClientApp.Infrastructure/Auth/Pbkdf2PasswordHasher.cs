using System.Security.Cryptography;
using OrderClientApp.Application.Abstractions.Auth;

namespace OrderClientApp.Infrastructure.Auth;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;

    public PasswordHashResult HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt.ToArray(),
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHashResult(
            Convert.ToBase64String(hashBytes),
            Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(hash)
            || string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        var saltBytes = Convert.FromBase64String(salt);
        var expectedHashBytes = Convert.FromBase64String(hash);
        var actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            expectedHashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }
}
