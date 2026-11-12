namespace Prixo.Application.Features.Portal;

public record RegisterPortalAccountRequest(string Uhid, string Mobile, DateOnly DateOfBirth, string Password);
public record PortalLoginRequest(string UhidOrMobile, string Password);
public record PortalRefreshRequest(string RefreshToken);
public record PortalChangePasswordRequest(string CurrentPassword, string NewPassword);

public record PortalPatientDto(long PatientId, string Uhid, string FullName, string Mobile, bool MustChangePassword);

public record PortalLoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken, PortalPatientDto Patient);
