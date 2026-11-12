using Microsoft.AspNetCore.Identity;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Infrastructure.Identity;

/// <summary>ASP.NET Identity PasswordHasher (PBKDF2, per-password salt, versioned format).</summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object User = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(User, hash, password)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
