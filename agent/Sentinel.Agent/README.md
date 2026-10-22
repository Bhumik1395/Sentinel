# Sentinel Windows Agent

Windows Service foundation for Phases 3–4. The service uses a 60-second heartbeat cadence and a
15-second telemetry delivery cycle.
The enrollment PFX is validated and stored in the LocalMachine `SentinelAgent` certificate store;
the MSI must grant the service identity read access to its private key. Policy retrieval, mTLS
Telemetry is collected from Sysmon Operational (process/network/DNS), Security 4688 process
creation, and PowerShell Operational 4104 script blocks, subject to the endpoint policy. Batches
are encrypted with DPAPI and retained until the API accepts them. Configure
`Sentinel:CertificateFingerprint` after enrollment; without it the agent deliberately sends nothing.
