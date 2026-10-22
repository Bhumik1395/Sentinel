# Sentinel agent certificate authority

Sentinel uses an internal two-tier private PKI: retain the root CA offline and provide only an
issuing-CA PFX to the API through the production secret manager. The PFX must never be committed,
placed in a Docker image, or stored in `backend/.env`.

Enrollment issues a unique 30-day client certificate whose private key is returned once over TLS.
The agent must store it using Windows machine-protected storage and rotate it before expiry.
