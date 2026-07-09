namespace Sentinel.Identity.Organizations;

public interface IKeycloakUserProvisioningService
{
    Task<Guid> ProvisionUserAsync(string email, string role, Guid organizationId);
}

public class StubKeycloakUserProvisioningService : IKeycloakUserProvisioningService
{
    public Task<Guid> ProvisionUserAsync(string email, string role, Guid organizationId)
        => Task.FromResult(Guid.NewGuid());
}
