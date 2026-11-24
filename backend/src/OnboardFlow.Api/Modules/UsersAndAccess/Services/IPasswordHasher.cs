namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

/// <summary>
/// Abstracts password hashing and verification, decoupling the auth layer
/// from any specific hashing implementation (SHA-256, BCrypt, Argon2, etc.).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
