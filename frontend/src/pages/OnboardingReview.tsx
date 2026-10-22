import { useCallback, useEffect, useState } from "react";
import { apiRequest } from "../lib/api";
import { useAuth } from "../lib/authContext";

type ApplicationStatus = "PENDING" | "IN_REVIEW" | "IN_DISCUSSION" | "APPROVED" | "REJECTED";
interface Application { id: string; companyName: string; contactName: string; contactEmail: string; requestedEndpointCap: number; notes: string | null; status: ApplicationStatus; createdAt: string; }
interface ApproveResult { organizationId: string; csoTemporaryPassword: string; }

const statusLabel: Record<ApplicationStatus, string> = { PENDING: "Pending", IN_REVIEW: "In review", IN_DISCUSSION: "In discussion", APPROVED: "Approved", REJECTED: "Rejected" };

export function OnboardingReview() {
    const auth = useAuth();
    const [applications, setApplications] = useState<Application[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [workingId, setWorkingId] = useState<string | null>(null);
    const [approved, setApproved] = useState<{ email: string; password: string } | null>(null);

    const load = useCallback(async () => {
        if (!auth.accessToken) return;
        setLoading(true);
        setError(null);
        try { setApplications(await apiRequest<Application[]>(auth.accessToken, "/applications")); }
        catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to load applications."); }
        finally { setLoading(false); }
    }, [auth.accessToken]);
    useEffect(() => { void load(); }, [load]);

    const transition = async (application: Application, action: "review" | "discuss" | "approve" | "reject") => {
        if (!auth.accessToken) return;
        let body: string | undefined;
        if (action === "reject") {
            const reason = window.prompt("Reason for rejection:");
            if (!reason?.trim()) return;
            body = JSON.stringify({ reason });
        }
        setWorkingId(application.id);
        setError(null);
        try {
            const result = await apiRequest<ApproveResult | undefined>(auth.accessToken, `/applications/${application.id}/${action}`, { method: "POST", body });
            if (action === "approve" && result) setApproved({ email: application.contactEmail, password: result.csoTemporaryPassword });
            await load();
        } catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to update application."); }
        finally { setWorkingId(null); }
    };

    return <div className="page-pad">
        <div className="page-heading"><div><h1 className="page-title">Onboarding review</h1><p className="page-description">Move customer applications through review, discussion, and owner approval.</p></div><button className="btn-secondary" onClick={() => void load()}>Refresh</button></div>
        {error && <div className="error-box">{error}</div>}
        {approved && <div className="credential-box"><strong>Application approved</strong><span>Send the initial CSO these one-time credentials securely.</span><span>Username: <code>{approved.email}</code> · Password: <code>{approved.password}</code></span><button className="btn-secondary" onClick={() => setApproved(null)}>I’ve recorded this</button></div>}
        <section className="panel">
            {loading ? <div className="skeleton-block" /> : applications.length === 0 ? <div className="empty-box"><p className="empty-title">No onboarding applications.</p><p className="empty-detail">New customer applications will appear here.</p></div> :
                <table className="data-table"><thead><tr><th>Company</th><th>Contact</th><th>Endpoint cap</th><th>Status</th><th>Received</th><th>Actions</th></tr></thead><tbody>
                    {applications.map((application) => <tr key={application.id}><td><strong>{application.companyName}</strong>{application.notes && <small className="table-note">{application.notes}</small>}</td><td>{application.contactName}<small className="table-note">{application.contactEmail}</small></td><td className="mono-cell">{application.requestedEndpointCap}</td><td><span className={`status status-${application.status.toLowerCase()}`}>{statusLabel[application.status]}</span></td><td>{new Date(application.createdAt).toLocaleDateString()}</td><td className="actions">
                        {application.status === "PENDING" && <button className="btn-secondary" disabled={workingId === application.id} onClick={() => void transition(application, "review")}>Review</button>}
                        {application.status === "IN_REVIEW" && <button className="btn-secondary" disabled={workingId === application.id} onClick={() => void transition(application, "discuss")}>Begin discussion</button>}
                        {application.status === "IN_DISCUSSION" && <><button className="btn-primary" disabled={workingId === application.id} onClick={() => void transition(application, "approve")}>Approve</button><button className="btn-danger" disabled={workingId === application.id} onClick={() => void transition(application, "reject")}>Reject</button></>}
                    </td></tr>)}
                </tbody></table>}
        </section>
    </div>;
}
