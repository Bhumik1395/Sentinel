import { useAuth } from "../lib/authContext";
import { apiRequest } from "../lib/api";
import { useLoadState } from "../lib/useLoadState";

interface Endpoint {
    id: string;
    hostname: string;
    agentVersion: string;
    policyVersion: number;
    status: "HEALTHY" | "DEGRADED" | "OFFLINE";
    lastHeartbeatAt: string | null;
    registeredAt: string;
}

async function fetchEndpoints(accessToken: string | null): Promise<Endpoint[]> {
    if (!accessToken) return [];
    return (await apiRequest<{ items: Endpoint[] }>(accessToken, "/endpoints")).items;
}

export function Endpoints() {
    const auth = useAuth();
    const { state, reload } = useLoadState(() => fetchEndpoints(auth.accessToken), (items) => items.length === 0, [auth.accessToken]);
    return <div className="page-pad">
        <div className="page-heading"><div><h1 className="page-title">Endpoints</h1><p className="page-description">Agent health and policy delivery across this organization.</p></div><button className="btn-secondary" onClick={() => reload(true)}>Refresh</button></div>
        {state.status === "loading" && <div className="skeleton-block" style={{ height: 260 }} />}
        {state.status === "error" && <div className="error-box"><p className="error-title">Unable to load endpoints.</p><p className="error-detail">{state.message}</p><button className="btn-primary" onClick={() => reload()}>Retry</button></div>}
        {state.status === "empty" && <div className="empty-box"><p className="empty-title">No endpoints registered yet.</p><p className="empty-detail">Enroll a Windows agent to begin collecting telemetry.</p></div>}
        {state.status === "ready" && <div className="table-wrap"><table className="data-table"><thead><tr><th>Endpoint</th><th>Health</th><th>Agent</th><th>Policy</th><th>Last heartbeat</th><th>Registered</th></tr></thead><tbody>{state.data.map((endpoint) => <tr key={endpoint.id}><td className="mono-cell">{endpoint.hostname}</td><td><span className={`status status-${endpoint.status.toLowerCase()}`}>{endpoint.status}</span></td><td>{endpoint.agentVersion}</td><td>v{endpoint.policyVersion}</td><td className="mono-cell">{endpoint.lastHeartbeatAt ? new Date(endpoint.lastHeartbeatAt).toLocaleString() : "Never"}</td><td className="mono-cell">{new Date(endpoint.registeredAt).toLocaleDateString()}</td></tr>)}</tbody></table></div>}
    </div>;
}
