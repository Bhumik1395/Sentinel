namespace Sentinel.Licensing;

public record License(
    Guid Id,
    Guid OrganizationId,
    int EndpointCap,
    int CurrentEndpointCount,
    string Status
);

public record RegisterEndpointRequest(string Hostname, string AgentVersion = "unknown");
public record CreateEnrollmentTokenRequest(Guid OrganizationId, int ExpiresInMinutes = 60);
public record RedeemEnrollmentTokenRequest(string Token, string Hostname, string AgentVersion = "unknown");

public enum RegistrationOutcome
{
    Registered,
    CapReached,
    LicenseSuspended,
    LicenseExpired
}

public record RegisterEndpointResult(RegistrationOutcome Outcome, Guid? EndpointId);

