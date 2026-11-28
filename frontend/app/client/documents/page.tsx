"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { listCaseDocuments } from "@/features/documents/api";
import { CaseDocumentListItem } from "@/features/documents/types";

interface CaseItemSummary {
  id: string;
  documentName: string;
  isRequired: boolean;
  status: string;
}

interface CaseDetailSummary {
  id: string;
  referenceCode?: string;
  items?: CaseItemSummary[];
}

export default function ClientDocumentsPage() {
  const [cases, setCases] = useState<CaseDetailSummary[]>([]);
  const [selectedCaseId, setSelectedCaseId] = useState<string>("");
  const [caseItems, setCaseItems] = useState<CaseItemSummary[]>([]);
  const [uploadedDocs, setUploadedDocs] = useState<CaseDocumentListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    const loadCases = async () => {
      setLoading(true);
      setError(null);
      try {
        const token = localStorage.getItem("onboardflow_token");
        const res = await fetch(`${apiUrl}/api/onboarding/cases`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!res.ok) {
          throw new Error("Could not load your onboarding cases.");
        }
        const raw = (await res.json()) as
          | CaseDetailSummary[]
          | { items?: CaseDetailSummary[] };
        const data = Array.isArray(raw) ? raw : raw.items ?? [];
        setCases(data);
        if (data.length > 0) {
          setSelectedCaseId(data[0].id);
        }
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load onboarding cases."
        );
      } finally {
        setLoading(false);
      }
    };

    loadCases();
  }, [apiUrl]);

  useEffect(() => {
    if (!selectedCaseId) return;

    const loadCaseDetailsAndDocs = async () => {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const [caseRes, docs] = await Promise.all([
          fetch(`${apiUrl}/api/onboarding/cases/${selectedCaseId}`, {
            headers: token ? { Authorization: `Bearer ${token}` } : {},
          }),
          listCaseDocuments(selectedCaseId).catch(() => []),
        ]);

        if (caseRes.ok) {
          const detail = (await caseRes.json()) as CaseDetailSummary;
          setCaseItems(detail.items ?? []);
        }
        setUploadedDocs(docs);
      } catch {
        setUploadedDocs([]);
      }
    };

    loadCaseDetailsAndDocs();
  }, [apiUrl, selectedCaseId]);

  const docsByItemId = new Map(
    uploadedDocs.map((d) => [d.checklistItemId, d])
  );

  return (
    <RoleGuard allowedRoles={["Client"]}>
      <WorkspaceLayout breadcrumb="Your documents">
        <div className="max-w-4xl mx-auto space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">
                Your Documents
              </h1>
              <p className="text-sm text-slate-600 mt-1">
                Upload required documents for your active onboarding checklist.
              </p>
            </div>
            {cases.length > 1 && (
              <select
                value={selectedCaseId}
                onChange={(e) => setSelectedCaseId(e.target.value)}
                className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
              >
                {cases.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.referenceCode || c.id}
                  </option>
                ))}
              </select>
            )}
          </div>

          {loading && (
            <p className="text-sm text-slate-500">Loading checklist items...</p>
          )}

          {error && (
            <div className="rounded-xl bg-red-50 border border-red-200 p-4 text-sm text-red-700">
              {error}
            </div>
          )}

          {!loading && !error && caseItems.length === 0 && (
            <div className="rounded-2xl bg-[#0d272d] text-slate-200 p-6 text-sm">
              No checklist items found for your onboarding case yet.
            </div>
          )}

          <div className="space-y-3">
            {caseItems.map((item) => {
              const doc = docsByItemId.get(item.id);
              return (
                <div
                  key={item.id}
                  className="rounded-2xl bg-[#0d272d] border border-[#18434d] p-4 sm:p-5 text-white flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-sm sm:text-base">
                        {item.documentName}
                      </span>
                      <span className="text-[11px] px-2 py-0.5 rounded-full bg-[#143d46] text-cyan-300">
                        {item.isRequired ? "Required" : "Optional"}
                      </span>
                    </div>
                    {doc ? (
                      <p className="text-xs text-emerald-300 mt-1">
                        Uploaded: {doc.fileName} (v{doc.currentVersionNumber})
                      </p>
                    ) : (
                      <p className="text-xs text-slate-400 mt-1">
                        Not uploaded yet
                      </p>
                    )}
                  </div>

                  {doc ? (
                    <span className="text-xs font-semibold text-emerald-300 px-3 py-1.5 rounded-lg bg-emerald-950/60 border border-emerald-700/50 self-start sm:self-center">
                      Submitted
                    </span>
                  ) : (
                    <Link
                      href={`/client/documents/upload?caseId=${encodeURIComponent(
                        selectedCaseId
                      )}&itemId=${encodeURIComponent(
                        item.id
                      )}&itemName=${encodeURIComponent(item.documentName)}`}
                      className="px-4 py-2 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-slate-950 text-xs font-semibold text-center transition-colors"
                    >
                      Upload document
                    </Link>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}
