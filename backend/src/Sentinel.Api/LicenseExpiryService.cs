using Sentinel.Licensing;
namespace Sentinel.Api;
public sealed class LicenseExpiryService(IServiceScopeFactory scopes, ILogger<LicenseExpiryService> logger) : BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
  using var timer=new PeriodicTimer(TimeSpan.FromHours(24));
  do { try { using var scope=scopes.CreateScope(); var changed=await scope.ServiceProvider.GetRequiredService<ILicenseService>().ExpireDueLicensesAsync(); if(changed>0)logger.LogInformation("Expired {Count} licenses",changed); } catch(Exception ex){logger.LogError(ex,"License expiry pass failed");} } while(await timer.WaitForNextTickAsync(stoppingToken));
 }
}
