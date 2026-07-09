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
    string Hostname
);

public enum RegistrationOutcome
{
    Registered,
    CapReached,
    LicenseSuspended
}

public record RegisterEndpointResult(RegistrationOutcome Outcome, Guid? EndpointId);

