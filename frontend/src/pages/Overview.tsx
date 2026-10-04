// frontend/src/pages/Overview.tsx — full replacement, now using the shared hook
import { useLoadState } from "../lib/useLoadState";

interface OverviewMetrics {
    totalEndpoints: number; healthyEndpoints: number; degradedEndpoints: number; offlineEndpoints: number;
    activeAlerts: number; criticalAlerts: number; highAlerts: number;
    openIncidents: number; investigatingIncidents: number; criticalIncidents: number;
}

async function fetchOverviewMetrics(): Promise<OverviewMetrics> {
    await new Promise((r) => setTimeout(r, 600));
    return {
        totalEndpoints: 0, healthyEndpoints: 0, degradedEndpoints: 0, offlineEndpoints: 0,
        activeAlerts: 0, criticalAlerts: 0, highAlerts: 0,
        openIncidents: 0, investigatingIncidents: 0, criticalIncidents: 0,
    };
}

export function Overview() {
    const { state, reload } = useLoadState(
        fetchOverviewMetrics,
        (m) => m.totalEndpoints === 0 && m.activeAlerts === 0 && m.openIncidents === 0
    );

    return (
        <div className="page-pad">
            <h1 className="page-title">Overview</h1>

            {state.status === "loading" && <MetricSkeleton />}

            {state.status === "error" && (
                <div className="error-box">
                    <p className="error-title">Unable to load overview data.</p>
                    <p className="error-detail">{state.message}</p>
                    <button className="btn-primary" onClick={reload}>Retry</button>
                </div>
            )}

            {state.status === "empty" && (
                <div className="empty-box">
                    <p className="empty-title">No endpoints registered yet.</p>
                    <p className="empty-detail">Install the Sentinel Agent on a Windows endpoint to start seeing activity here.</p>
                </div>
            )}

            {state.status === "ready" && <MetricStrip data={state.data} />}
        </div>
    );
}

function MetricSkeleton() {
    return (
        <div className="metric-strip">
            {Array.from({ length: 4 }).map((_, i) => <div key={i} className="skeleton-block" />)}
        </div>
    );
}

function MetricStrip({ data }: { data: OverviewMetrics }) {
    const cells = [
        { label: "Endpoints", value: data.totalEndpoints, sub: `${data.degradedEndpoints} degraded · ${data.offlineEndpoints} offline` },
        { label: "Active Alerts", value: data.activeAlerts, sub: `${data.criticalAlerts} critical · ${data.highAlerts} high`, critical: data.criticalAlerts > 0 },
        { label: "Open Incidents", value: data.openIncidents, sub: `${data.investigatingIncidents} investigating`, critical: data.criticalIncidents > 0 },
        { label: "Critical Incidents", value: data.criticalIncidents, sub: "requires attention", critical: data.criticalIncidents > 0 },
    ];
    return (
        <div className="metric-strip">
            {cells.map((c) => (
                <div key={c.label} className="metric-cell">
                    <div className={`metric-value${c.critical ? " metric-value-critical" : ""}`}>{c.value}</div>
                    <div className="metric-label">{c.label}</div>
                    <div className="metric-sub">{c.sub}</div>
                </div>
            ))}
        </div>
    );
}