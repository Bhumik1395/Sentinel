namespace Sentinel.Licensing;

public record License(
    Guid Id,
    Guid OrganizationId,
    int EndpointCap,
    int CurrentEndpointCount,
    string Status
);

public record RegisterEndpointRequest(
    Guid OrganizationId,
    string Hostname,
    string AgentVersion = "unknown"
);

public enum RegistrationOutcome
{
    Registered,
    CapReached,
    LicenseSuspended,
    LicenseExpired
}

public record RegisterEndpointResult(RegistrationOutcome Outcome, Guid? EndpointId);

