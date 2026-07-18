namespace Sentinel.Identity.Organizations;

public class StubKeycloakAdminProvisioningService : IKeycloakAdminProvisioningService
{
    public Task<ProvisionedUser> ProvisionUserWithPasswordAsync(
        string email,
        string role,
        Guid organizationId)
    {
        return Task.FromResult(
            new ProvisionedUser(
                Guid.NewGuid(),
                "TempPassword123!"
            ));
    }
}