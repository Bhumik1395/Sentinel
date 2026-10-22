// frontend/src/App.tsx — full replacement
import { NavLink as RouterNavLink, Route, Routes } from "react-router-dom";
import { useState } from "react";
import { useAuth, type Role } from "./lib/authContext";
import { Overview } from "./pages/Overview";
import { Alerts } from "./pages/Alerts";
import { Organizations } from "./pages/Organizations";
import { OnboardingReview } from "./pages/OnboardingReview";
import { Endpoints } from "./pages/Endpoints";
import { Incidents } from "./pages/Incidents";
import { Soar } from "./pages/Soar";

interface NavItem { label: string; path: string; roles: Role[] }

const NAV: NavItem[] = [
    { label: "Overview", path: "/overview", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Alerts", path: "/alerts", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Incidents", path: "/incidents", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Endpoints", path: "/endpoints", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "SOAR", path: "/soar", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Organizations", path: "/organizations", roles: ["owner"] },
    { label: "Support Queue", path: "/support-queue", roles: ["owner", "support-team"] },
    { label: "Onboarding Review", path: "/onboarding-review", roles: ["owner", "support-team"] },
];

function NavLink({ item }: { item: NavItem }) {
    return (
        <RouterNavLink to={item.path} className={({ isActive }) => `nav-link${isActive ? " nav-link-active" : ""}`}>
            {item.label}
        </RouterNavLink>
    );
}

export default function App() {
    const auth = useAuth();
    if (!auth.isAuthenticated) return <SignIn />;
    const visibleNav = NAV.filter((item) => item.roles.includes(auth.role));

    return (
        <div className="app-shell">
            <nav className="app-nav">
                <div className="app-nav-scope">
                    {auth.isPlatformTier ? "Sentinel Company" : `Org ${auth.organizationId}`}
                </div>
                {visibleNav.map((item) => <NavLink key={item.path} item={item} />)}
            </nav>
            <main className="app-main">
                <header className="app-header">
                    <span className="app-role">{auth.role}</span>
                    <span className="app-user">{auth.userEmail}</span>
                </header>
                <Routes>
                    <Route path="/" element={<Overview />} />
                    <Route path="/overview" element={<Overview />} />
                    <Route path="/alerts" element={<Alerts />} />
                    <Route path="/organizations" element={<Organizations />} />
                    <Route path="/onboarding-review" element={<OnboardingReview />} />
                    <Route path="/endpoints" element={<Endpoints />} />
                    <Route path="/incidents" element={<Incidents />} />
                    <Route path="/soar" element={<Soar />} />
                </Routes>
            </main>
        </div>
    );
}

function SignIn() {
    const auth = useAuth();
    const [error, setError] = useState<string | null>(null);
    const [working, setWorking] = useState(false);

    const signIn = async () => {
        setError(null);
        setWorking(true);
        try {
            await auth.signIn();
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : "Unable to start sign-in.");
        } finally {
            setWorking(false);
        }
    };

    return <main className="sign-in-shell">
        <section className="sign-in-card">
            <p className="eyebrow">SENTINEL CONSOLE</p>
            <h1>Security operations, with a clear line of sight.</h1>
            <p>Sign in securely through your organization’s Sentinel identity provider.</p>
            {error && <p className="error-detail">{error}</p>}
            <button className="btn-primary" type="button" disabled={working} onClick={signIn}>
                {working ? "Complete sign-in in your browser…" : "Sign in"}
            </button>
        </section>
    </main>;
}
