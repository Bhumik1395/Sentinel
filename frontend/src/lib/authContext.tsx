import { createContext, useContext, useState, type ReactNode } from "react";

export type Role = "owner" | "support-team" | "cso" | "security-administrator" | "security-analyst";

export interface AuthContextValue {
    role: Role;
    organizationId: string | null; // null for Owner/Support Team (platform tier)
    userEmail: string;
    isPlatformTier: boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function MockAuthProvider({ children, as = "security-analyst" as Role }: { children: ReactNode; as?: Role }) {
    const [value] = useState<AuthContextValue>({
        role: as,
        organizationId: as === "owner" || as === "support-team" ? null : "org-demo-001",
        userEmail: "analyst@acme-security.test",
        isPlatformTier: as === "owner" || as === "support-team",
    });
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
    return ctx;
}