import { useEffect, useState } from "react";
import { apiRequest } from "../lib/api";
import { useAuth } from "../lib/authContext";

type Incident = { id: string; title: string; severity: string; status: string; openedAt: string; summary?: string };
export function Incidents() {
  const auth=useAuth(); const [items,setItems]=useState<Incident[]>([]); const [error,setError]=useState<string>(); const [loading,setLoading]=useState(true);
  useEffect(()=>{if(!auth.accessToken)return;apiRequest<{items:Incident[]}>(auth.accessToken,"/incidents").then(x=>setItems(x.items)).catch(e=>setError(e.message)).finally(()=>setLoading(false));},[auth.accessToken]);
  return <section className="page-pad"><p className="eyebrow">INVESTIGATION</p><h1 className="page-title">Incidents</h1>{loading?<p>Loading incidents…</p>:error?<p className="error-detail">{error}</p>:items.length===0?<p className="empty-detail">No incidents are open. Convert a validated alert into an incident when investigation needs a shared record.</p>:<div className="data-list">{items.map(i=><article key={i.id} className="data-card"><strong>{i.title}</strong><span>{i.severity} · {i.status}</span><small>{new Date(i.openedAt).toLocaleString()}</small></article>)}</div>}</section>;
}
