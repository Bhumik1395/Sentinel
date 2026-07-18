namespace Sentinel.Identity.Organizations;

public record Organization(Guid Id, string Type, string Name, DateTimeOffset CreatedAt);

public record CreateOrganizationRequest(string Name, string CsoEmail, int EndpointCap);

public record CreateOrganizationResult(Guid OrganizationId, Guid CsoUserId, Guid LicenseId, string CsoTemporaryPassword);