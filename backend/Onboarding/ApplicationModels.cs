// backend/src/Sentinel.Identity/Onboarding/ApplicationModels.cs
namespace Sentinel.Identity.Onboarding;

public record SubmitApplicationRequest(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    int RequestedEndpointCap,
    string? Notes);

public record Application(
    Guid Id,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    int RequestedEndpointCap,
    string? Notes,
    string Status,
    Guid? CreatedOrganizationId,
    DateTimeOffset CreatedAt);

public record ApproveApplicationResult(
    Guid OrganizationId,
    Guid CsoUserId,
    Guid LicenseId,
    string TemporaryPassword);
