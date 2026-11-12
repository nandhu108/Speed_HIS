namespace Prixo.Application.Features.Admin.Auth;

public record LoginRequest(string Username, string Password, long? BranchId);

public record BranchOption(long Id, string Code, string Name);

public record AuthUserDto(
    long Id, string Username, string FullName, string? Email, string? PhotoPath,
    long BranchId, string BranchName, bool MustChangePassword,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions,
    string? DefaultLandingPath);

public record LoginResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken,
    AuthUserDto User);

public record RefreshRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
