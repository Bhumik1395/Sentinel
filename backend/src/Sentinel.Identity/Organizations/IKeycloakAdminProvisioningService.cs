namespace Sentinel.Identity.Organizations;

public sealed record ProvisionedUser(
    Guid KeycloakId,
    string TemporaryPassword);

public interface IKeycloakAdminProvisioningService
{
    Task<ProvisionedUser> ProvisionUserWithPasswordAsync(
        string email,
        string role,
        Guid organizationId);
}