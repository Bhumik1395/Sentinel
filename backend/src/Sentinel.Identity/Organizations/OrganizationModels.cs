namespace Sentinel.Identity.Organizations;

public record Organization(Guid Id, string Name, string Slug, DateTimeOffset CreatedAt);

public record CreateOrganizationRequest(string Name, string CsoEmail, int EndpointCap);

public record CreateOrganizationResult(Guid OrganizationId, Guid CsoUserId, Guid LicenseId, string CsoTemporaryPassword);
