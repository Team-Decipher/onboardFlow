using OnboardFlow.Api.Data;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

/// <summary>
/// Delegates to <see cref="DbSeeder"/> so the existing hash algorithm (SHA-256 + Base64)
/// is reused — no data migration needed. Swap this implementation for BCrypt/Argon2
/// by registering a different <see cref="IPasswordHasher"/> in DI.
/// </summary>
public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => DbSeeder.HashPassword(password);
    public bool Verify(string password, string storedHash) => DbSeeder.VerifyPassword(password, storedHash);
}
