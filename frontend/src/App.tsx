// frontend/src/App.tsx — full replacement
import { NavLink as RouterNavLink, Route, Routes } from "react-router-dom";
import { useAuth, type Role } from "./lib/authContext";
import { Overview } from "./pages/Overview";
import { Alerts } from "./pages/Alerts";

interface NavItem { label: string; path: string; roles: Role[] }

const NAV: NavItem[] = [
    { label: "Overview", path: "/overview", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Alerts", path: "/alerts", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Incidents", path: "/incidents", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Threat Hunting", path: "/hunting", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Attack Graph", path: "/graph", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Endpoints", path: "/endpoints", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "SOAR", path: "/soar", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Deception", path: "/deception", roles: ["security-administrator"] },
    { label: "Reports", path: "/reports", roles: ["cso", "security-administrator", "security-analyst"] },
    { label: "Users", path: "/users", roles: ["cso", "security-administrator"] },
    { label: "Settings", path: "/settings", roles: ["cso", "security-administrator"] },
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

function NotYetBuilt({ label }: { label: string }) {
    return (
        <div className="page-pad">
            <h1 className="page-title">{label}</h1>
            <p className="empty-detail">Not built yet.</p>
        </div>
    );
}

export default function App() {
    const auth = useAuth();
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
                    {NAV.filter((n) => n.path !== "/overview" && n.path !== "/alerts").map((n) => (
                        <Route key={n.path} path={n.path} element={<NotYetBuilt label={n.label} />} />
                    ))}
                </Routes>
            </main>
        </div>
    );
}