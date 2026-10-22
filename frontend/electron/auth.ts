import { createHash, randomBytes } from "node:crypto";
import { createServer } from "node:http";
import { URL, URLSearchParams } from "node:url";
import { shell } from "electron";

export interface TokenSet {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
}

const loopbackRedirect = "http://127.0.0.1:53682/callback";

function base64Url(bytes: Buffer) {
    return bytes.toString("base64").replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

function keycloakUrl() {
    return process.env.KEYCLOAK_URL ?? "http://localhost:8080";
}

/**
 * Native applications must authenticate in the user's system browser. This
 * implements authorization-code + PKCE and only accepts the exact loopback
 * callback registered with Keycloak.
 */
export async function signIn(): Promise<TokenSet> {
    const verifier = base64Url(randomBytes(48));
    const challenge = base64Url(createHash("sha256").update(verifier).digest());
    const state = base64Url(randomBytes(32));
    const callback = await waitForCallback(state);

    const authorize = new URL(`/realms/sentinel/protocol/openid-connect/auth`, keycloakUrl());
    authorize.search = new URLSearchParams({
        client_id: "sentinel-desktop",
        redirect_uri: loopbackRedirect,
        response_type: "code",
        // A regular refresh token is sufficient for the current in-memory session.
        // Offline tokens require an additional Keycloak role and are added with the
        // encrypted token store in the next session-persistence pass.
        scope: "openid profile email",
        code_challenge: challenge,
        code_challenge_method: "S256",
        state,
    }).toString();

    // Do not wait for the OS shell handler. On Windows that promise can remain
    // pending while the default browser is being activated, which used to leave
    // the renderer looking frozen even though the authorization server was ready.
    void shell.openExternal(authorize.toString(), { activate: true });
    const code = await callback;

    const response = await fetch(new URL(`/realms/sentinel/protocol/openid-connect/token`, keycloakUrl()), {
        method: "POST",
        headers: { "content-type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({
            grant_type: "authorization_code",
            client_id: "sentinel-desktop",
            code,
            redirect_uri: loopbackRedirect,
            code_verifier: verifier,
        }),
    });

    if (!response.ok) {
        throw new Error(`Keycloak token exchange failed (${response.status}).`);
    }

    const payload = await response.json() as {
        access_token?: string; refresh_token?: string; expires_in?: number;
    };
    if (!payload.access_token || !payload.refresh_token) {
        throw new Error("Keycloak did not return the required access and refresh tokens.");
    }

    return {
        accessToken: payload.access_token,
        refreshToken: payload.refresh_token,
        expiresIn: payload.expires_in ?? 0,
    };
}

function waitForCallback(expectedState: string): Promise<string> {
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => finish(new Error("Sign-in timed out. Please try again.")), 5 * 60_000);
        const server = createServer((request, response) => {
            const requestUrl = new URL(request.url ?? "/", loopbackRedirect);
            if (requestUrl.pathname !== "/callback") {
                response.writeHead(404).end();
                return;
            }

            const error = requestUrl.searchParams.get("error");
            const state = requestUrl.searchParams.get("state");
            const code = requestUrl.searchParams.get("code");
            if (error || state !== expectedState || !code) {
                response.writeHead(400, { "content-type": "text/html; charset=utf-8" });
                response.end("<h1>Sentinel sign-in failed</h1><p>You can close this tab and try again.</p>");
                finish(new Error(error ? `Keycloak sign-in failed: ${error}.` : "Invalid sign-in callback."));
                return;
            }

            response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
            response.end("<h1>Signed in to Sentinel</h1><p>You can close this tab and return to the desktop app.</p>");
            finish(undefined, code);
        });

        const finish = (error?: Error, code?: string) => {
            clearTimeout(timeout);
            server.close();
            error ? reject(error) : resolve(code!);
        };

        server.once("error", (error) => finish(new Error(`Cannot listen for the sign-in callback: ${error.message}`)));
        server.listen(53682, "127.0.0.1");
    });
}
