using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Prixo.Infrastructure.Identity;

namespace Prixo.Application.Tests.Identity;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(
        Options.Create(new JwtOptions
        {
            Key = "unit-test-signing-key-at-least-32-characters-long",
            Issuer = "PrixoTests",
            Audience = "PrixoTests",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
        }),
        TimeProvider.System);

    [Fact]
    public void CreateAccessToken_EmbedsIdentityClaims_ButNoPermissions()
    {
        var service = CreateService();
        var sessionId = Guid.NewGuid();

        var result = service.CreateAccessToken(42, "jdoe", "John Doe", 7, sessionId, ["SUPER_ADMIN"]);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.Token);
        token.GetClaim("sub").Value.Should().Be("42");
        token.GetClaim("branch").Value.Should().Be("7");
        token.GetClaim("sid").Value.Should().Be(sessionId.ToString());
        token.Claims.Should().NotContain(c => c.Type.Contains("permission", StringComparison.OrdinalIgnoreCase),
            "permissions are checked against the database, never carried in the JWT");
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(14));
    }

    [Fact]
    public void CreateRefreshToken_HashMatchesToken_AndTokensAreUnique()
    {
        var service = CreateService();

        var (token1, hash1) = service.CreateRefreshToken();
        var (token2, _) = service.CreateRefreshToken();

        token1.Should().NotBe(token2);
        service.HashRefreshToken(token1).Should().Be(hash1);
        hash1.Should().NotContain(token1, "only the hash may be stored");
    }
}
