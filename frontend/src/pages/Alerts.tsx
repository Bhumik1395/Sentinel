// frontend/src/pages/Alerts.tsx
// PRD §18b: Severity, Alert Name, Endpoint, User, Detection Source, MITRE
// Technique, Timestamp, Status. Search/filter/sort/paginate. TRD §24b: 5s
// poll interval - alerts are the one page where that's a hard product
// requirement (PRD Goal 1: high-severity visible within 5s end-to-end),
// not a nicety, so polling is built in from the start rather than added later.
import { useEffect, useMemo, useState } from "react";
import { useLoadState } from "../lib/useLoadState";

type Severity = "INFORMATIONAL" | "LOW" | "MEDIUM" | "HIGH" | "CRITICAL";

interface Alert {
    id: string;
    severity: Severity;
    name: string;
    endpoint: string;
    user: string;
    detectionSource: string;
    mitreTechnique: string;
    timestamp: string;
    status: "OPEN" | "ACKNOWLEDGED" | "IN_INVESTIGATION" | "RESOLVED" | "FALSE_POSITIVE";
    occurrenceCount: number;
}

const SEVERITY_ORDER: Severity[] = ["CRITICAL", "HIGH", "MEDIUM", "LOW", "INFORMATIONAL"];
const PAGE_SIZE = 25;

// Stand-in for GET /alerts. Swap for a real fetch once the backend endpoint
// exists - the table/filter/sort/pagination code below never needs to change.
async function fetchAlerts(): Promise<Alert[]> {
    await new Promise((r) => setTimeout(r, 400));
    return [];
}

function SeverityBadge({ severity }: { severity: Severity }) {
    return (
        <span className={`sev-badge sev-${severity.toLowerCase()}`}>
            {severity[0]}{severity.slice(1).toLowerCase()}
        </span>
    );
}

export function Alerts() {
    const { state, reload } = useLoadState(fetchAlerts, (a) => a.length === 0);
    const [search, setSearch] = useState("");
    const [severityFilter, setSeverityFilter] = useState<Severity | "ALL">("ALL");
    const [sortBy, setSortBy] = useState<"severity" | "timestamp">("severity");
    const [page, setPage] = useState(0);

    // PRD/TRD: alerts poll every 5 seconds. Silent background refresh, not a
    // full loading-state reset - don't show the skeleton again just because
    // the poll ticked, that would be visually disruptive on a page an
    // analyst is actively reading.
    useEffect(() => {
        const interval = setInterval(reload, 5000);
        return () => clearInterval(interval);
    }, [reload]);

    const alerts = state.status === "ready" ? state.data : [];

    const filtered = useMemo(() => {
        return alerts
            .filter((a) => severityFilter === "ALL" || a.severity === severityFilter)
            .filter((a) =>
                search === "" ||
                a.name.toLowerCase().includes(search.toLowerCase()) ||
                a.endpoint.toLowerCase().includes(search.toLowerCase()) ||
                a.user.toLowerCase().includes(search.toLowerCase())
            )
            .sort((a, b) =>
                sortBy === "severity"
                    ? SEVERITY_ORDER.indexOf(a.severity) - SEVERITY_ORDER.indexOf(b.severity)
                    : new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
            );
    }, [alerts, search, severityFilter, sortBy]);

    const pageCount = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const pageRows = filtered.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

    return (
        <div className="page-pad">
            <h1 className="page-title">Alerts</h1>

            <div className="table-controls">
                <input
                    className="search-input"
                    placeholder="Search by alert, endpoint, or user"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(0); }}
                />
                <select
                    className="filter-select"
                    value={severityFilter}
                    onChange={(e) => { setSeverityFilter(e.target.value as Severity | "ALL"); setPage(0); }}
                >
                    <option value="ALL">All severities</option>
                    {SEVERITY_ORDER.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
                <select className="filter-select" value={sortBy} onChange={(e) => setSortBy(e.target.value as "severity" | "timestamp")}>
                    <option value="severity">Sort by severity</option>
                    <option value="timestamp">Sort by newest</option>
                </select>
            </div>

            {state.status === "loading" && <div className="skeleton-block" style={{ height: 300 }} />}

            {state.status === "error" && (
                <div className="error-box">
                    <p className="error-title">Unable to load alerts.</p>
                    <p className="error-detail">{state.message}</p>
                    <button className="btn-primary" onClick={reload}>Retry</button>
                </div>
            )}

            {state.status === "empty" && (
                <div className="empty-box">
                    <p className="empty-title">No alerts yet.</p>
                    <p className="empty-detail">Alerts will appear here as soon as the detection engine flags activity on a registered endpoint.</p>
                </div>
            )}

            {state.status === "ready" && (
                <>
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Severity</th><th>Alert</th><th>Endpoint</th><th>User</th>
                                <th>Source</th><th>MITRE</th><th>Time</th><th>Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pageRows.map((a) => (
                                <tr key={a.id}>
                                    <td><SeverityBadge severity={a.severity} /></td>
                                    <td>{a.name}{a.occurrenceCount > 1 && <span className="occurrence-count"> ×{a.occurrenceCount}</span>}</td>
                                    <td className="mono-cell">{a.endpoint}</td>
                                    <td>{a.user}</td>
                                    <td>{a.detectionSource}</td>
                                    <td className="mono-cell">{a.mitreTechnique}</td>
                                    <td className="mono-cell">{new Date(a.timestamp).toLocaleTimeString()}</td>
                                    <td>{a.status.replace("_", " ")}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    <div className="pagination">
                        <button className="btn-secondary" disabled={page === 0} onClick={() => setPage((p) => p - 1)}>Previous</button>
                        <span className="page-indicator">Page {page + 1} of {pageCount}</span>
                        <button className="btn-secondary" disabled={page >= pageCount - 1} onClick={() => setPage((p) => p + 1)}>Next</button>
                    </div>
                </>
            )}
        </div>
    );
}