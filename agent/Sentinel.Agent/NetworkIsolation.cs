using System.Diagnostics;
using System.Net;

/// <summary>Fail-closed Windows Firewall isolation that preserves only the configured Sentinel API route.</summary>
public sealed class NetworkIsolation(IConfiguration configuration, ILogger<NetworkIsolation> logger)
{
 private const string Group="Sentinel Endpoint Isolation";
 public async Task<object> IsolateAsync(CancellationToken ct)
 {
  var value=configuration["Sentinel:ApiBaseUrl"];if(!Uri.TryCreate(value,UriKind.Absolute,out var api)||string.IsNullOrWhiteSpace(api.Host))throw new InvalidOperationException("A valid Sentinel:ApiBaseUrl is required for isolation.");
  var addresses=await Dns.GetHostAddressesAsync(api.Host,ct);if(addresses.Length==0)throw new InvalidOperationException("Sentinel API host could not be resolved.");
  await Run("advfirewall",$"set allprofiles firewallpolicy blockinbound,blockoutbound",ct);
  foreach(var ip in addresses)await Run("advfirewall",$"firewall add rule name=\"Sentinel API {ip}\" dir=out action=allow remoteip={ip} protocol=TCP remoteport={api.Port} group=\"{Group}\"",ct);
  return new { isolated=true,apiHost=api.Host,allowedAddresses=addresses.Select(x=>x.ToString()).ToArray() };
 }
 public async Task<object> RestoreAsync(CancellationToken ct){await Run("advfirewall",$"firewall delete rule group=\"{Group}\"",ct);await Run("advfirewall","set allprofiles firewallpolicy blockinbound,allowoutbound",ct);return new{isolated=false};}
 private async Task Run(string file,string args,CancellationToken ct){using var p=Process.Start(new ProcessStartInfo(file,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true});if(p is null)throw new InvalidOperationException("Unable to start Windows Firewall.");await p.WaitForExitAsync(ct);if(p.ExitCode!=0){var error=await p.StandardError.ReadToEndAsync(ct);logger.LogError("Firewall command failed: {Error}",error);throw new InvalidOperationException("Windows Firewall update failed.");}}
}
