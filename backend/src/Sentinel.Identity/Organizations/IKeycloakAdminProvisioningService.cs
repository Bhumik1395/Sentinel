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
    Task DeleteUserAsync(Guid keycloakUserId);
    Task SetUserEnabledAsync(Guid keycloakUserId, bool enabled);
}
