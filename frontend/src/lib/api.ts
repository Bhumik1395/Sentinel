const apiBaseUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5226";

export class ApiError extends Error {
    constructor(message: string, public readonly status: number) {
        super(message);
    }
}

export async function apiRequest<T>(accessToken: string, path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Authorization", `Bearer ${accessToken}`);
    if (init.body) headers.set("Content-Type", "application/json");

    const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers });
    if (!response.ok) {
        let message = `Request failed (${response.status}).`;
        try {
            const body = await response.json() as { error?: string; detail?: string; title?: string };
            message = body.error ?? body.detail ?? body.title ?? message;
        } catch {
            // A proxy or older API might return an empty response body.
        }
        throw new ApiError(message, response.status);
    }
    if (response.status === 204) return undefined as T;
    return response.json() as Promise<T>;
}
