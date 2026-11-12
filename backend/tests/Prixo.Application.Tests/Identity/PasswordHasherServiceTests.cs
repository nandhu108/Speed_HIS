using FluentAssertions;
using Prixo.Infrastructure.Identity;

namespace Prixo.Application.Tests.Identity;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void Hash_ThenVerify_Succeeds()
    {
        var hash = _hasher.Hash("Admin@123");
        _hasher.Verify(hash, "Admin@123").Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var hash = _hasher.Hash("Admin@123");
        _hasher.Verify(hash, "admin@123").Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Per-password salt: equal inputs must not produce equal hashes.
        _hasher.Hash("Admin@123").Should().NotBe(_hasher.Hash("Admin@123"));
    }
}
