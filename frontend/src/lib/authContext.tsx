import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

export type Role = "owner" | "support-team" | "cso" | "security-administrator" | "security-analyst";

export interface AuthContextValue {
    isAuthenticated: boolean;
    isLoading: boolean;
    role: Role;
    organizationId: string | null; // null for Owner/Support Team (platform tier)
    userEmail: string;
    isPlatformTier: boolean;
    accessToken: string | null;
    signIn: () => Promise<void>;
    signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface TokenClaims {
    email?: string;
    preferred_username?: string;
    organizationId?: string;
    realm_access?: { roles?: string[] };
}

const roles: Role[] = ["owner", "support-team", "cso", "security-administrator", "security-analyst"];

function decodeClaims(token: string): TokenClaims {
    const payload = token.split(".")[1];
    if (!payload) throw new Error("The access token is malformed.");
    return JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/"))) as TokenClaims;
}

function contextFromToken(accessToken: string): Omit<AuthContextValue, "isLoading" | "signIn" | "signOut"> {
    const claims = decodeClaims(accessToken);
    const role = roles.find((candidate) => claims.realm_access?.roles?.includes(candidate));
    if (!role) throw new Error("Your Sentinel account has no recognized role.");
    const isPlatformTier = role === "owner" || role === "support-team";
    return {
        isAuthenticated: true,
        role,
        organizationId: isPlatformTier ? null : claims.organizationId ?? null,
        userEmail: claims.email ?? claims.preferred_username ?? "Unknown user",
        isPlatformTier,
        accessToken,
    };
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<Omit<AuthContextValue, "isLoading" | "signIn" | "signOut"> | null>(null);

    const value = useMemo<AuthContextValue>(() => ({
        ...(session ?? {
            isAuthenticated: false,
            role: "security-analyst" as Role,
            organizationId: null,
            userEmail: "",
            isPlatformTier: false,
            accessToken: null,
        }),
        isLoading: false,
        signIn: async () => {
            if (!window.sentinelAuth) {
                throw new Error("Sign-in is available in the Sentinel desktop application.");
            }
            const tokens = await window.sentinelAuth.startLogin();
            setSession(contextFromToken(tokens.accessToken));
        },
        signOut: () => setSession(null),
    }), [session]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
    return ctx;
}
