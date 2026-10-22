import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest } from "../lib/api";
import { useAuth } from "../lib/authContext";

interface Organization {
    id: string;
    name: string;
    slug: string;
    createdAt: string;
}

interface CreateOrganizationResult {
    organizationId: string;
    csoUserId: string;
    licenseId: string;
    csoTemporaryPassword: string;
}

export function Organizations() {
    const auth = useAuth();
    const [organizations, setOrganizations] = useState<Organization[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [creating, setCreating] = useState(false);
    const [credentials, setCredentials] = useState<{ email: string; password: string } | null>(null);

    const load = useCallback(async () => {
        if (!auth.accessToken) return;
        setLoading(true);
        setError(null);
        try {
            setOrganizations(await apiRequest<Organization[]>(auth.accessToken, "/organizations"));
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : "Unable to load organizations.");
        } finally {
            setLoading(false);
        }
    }, [auth.accessToken]);

    useEffect(() => { void load(); }, [load]);

    const create = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!auth.accessToken) return;
        const form = new FormData(event.currentTarget);
        setCreating(true);
        setError(null);
        try {
            const email = String(form.get("csoEmail"));
            const result = await apiRequest<CreateOrganizationResult>(auth.accessToken, "/organizations", {
                method: "POST",
                body: JSON.stringify({
                    name: String(form.get("name")),
                    csoEmail: email,
                    endpointCap: Number(form.get("endpointCap")),
                }),
            });
            event.currentTarget.reset();
            setCredentials({ email, password: result.csoTemporaryPassword });
            await load();
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : "Unable to create the organization.");
        } finally {
            setCreating(false);
        }
    };

    return <div className="page-pad">
        <div className="page-heading"><div><h1 className="page-title">Organizations</h1><p className="page-description">Create and oversee customer organizations and their initial CSO account.</p></div></div>
        {error && <div className="error-box">{error}</div>}
        {credentials && <div className="credential-box">
            <strong>CSO account created</strong>
            <span>Username: <code>{credentials.email}</code></span>
            <span>One-time password: <code>{credentials.password}</code></span>
            <button className="btn-secondary" onClick={() => setCredentials(null)}>I’ve recorded this</button>
        </div>}
        <section className="management-grid">
            <form className="panel form-stack" onSubmit={create}>
                <h2>Create organization</h2>
                <label>Organization name<input name="name" required maxLength={200} /></label>
                <label>Initial CSO email<input name="csoEmail" required type="email" maxLength={320} /></label>
                <label>Endpoint cap<input name="endpointCap" required type="number" min="1" max="1000000" defaultValue="100" /></label>
                <button className="btn-primary" disabled={creating}>{creating ? "Creating…" : "Create organization"}</button>
            </form>
            <section className="panel">
                <h2>Customer organizations</h2>
                {loading ? <div className="skeleton-block" /> : organizations.length === 0 ? <p className="empty-detail">No customer organizations have been created.</p> :
                    <table className="data-table"><thead><tr><th>Name</th><th>Slug</th><th>Created</th></tr></thead><tbody>
                        {organizations.map((organization) => <tr key={organization.id}><td>{organization.name}</td><td className="mono-cell">{organization.slug}</td><td>{new Date(organization.createdAt).toLocaleDateString()}</td></tr>)}
                    </tbody></table>}
            </section>
        </section>
    </div>;
}
