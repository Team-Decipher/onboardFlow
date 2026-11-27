"use client";

import React, { Suspense, useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";

interface CaseItem {
  id: string;
  documentName: string;
  isRequired: boolean;
  status: string;
  reviewerName?: string;
  dueDate?: string;
  uploadedFileName?: string;
}

interface CaseDetail {
  id: string;
  referenceCode: string;
  clientOrganisationName: string;
  clientTypeName: string;
  caseOwnerName: string;
  status: string;
  targetCompletionDate: string;
  createdAtUtc: string;
  items: CaseItem[];
}

function CaseDetailContent() {
  const router = useRouter();
  const params = useParams();
  const id = params?.id as string;

  const [caseData, setCaseData] = useState<CaseDetail | null>(null);
  const [loading, setLoading] = useState(true);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    if (!id) return;

    async function loadCase() {
      try {
        setLoading(true);
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        // Try /api/cases/{id} or fallback to /api/onboarding/cases/{id}
        let res = await fetch(`${apiUrl}/api/cases/${id}`, { headers }).catch(() => null);
        if (!res || !res.ok) {
          res = await fetch(`${apiUrl}/api/onboarding/cases/${id}`, { headers }).catch(() => null);
        }

        if (res && res.ok) {
          const data = await res.json();
          setCaseData(data);
        }
      } catch {
        // Fallback default view
      } finally {
        setLoading(false);
      }
    }

    loadCase();
  }, [id, apiUrl]);

  return (
    <WorkspaceLayout>
      <div className="flex-1 bg-[#f4f7f9] min-w-0 p-8 text-slate-800">
        <div className="max-w-6xl mx-auto space-y-6">
          {/* Breadcrumb */}
          <div className="flex items-center gap-1.5 text-xs text-slate-500 pb-2 border-b border-slate-200 font-medium">
            <Link href="/cases" className="hover:text-slate-800">
              Onboarding Cases
            </Link>
            <span>/</span>
            <span className="text-slate-700 font-semibold">{caseData?.referenceCode || id}</span>
          </div>

          {/* Header */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <div className="text-[11px] font-bold tracking-wider text-blue-700 uppercase mb-1">
                ONBOARDING CASE DETAILS
              </div>
              <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">
                {caseData?.referenceCode || "Case Detail"}
              </h1>
              <p className="text-xs text-slate-500 mt-1 font-medium">
                {caseData?.clientOrganisationName || "Client Organisation"} · Case ID: {id}
              </p>
            </div>

            <button
              type="button"
              onClick={() => router.push("/cases")}
              className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold rounded-lg border border-slate-300 shadow-2xs transition self-start sm:self-auto cursor-pointer"
            >
              ← Back to Case register
            </button>
          </div>

          {/* Case Detail Card */}
          <div className="bg-white rounded-2xl p-6 border border-slate-200/90 shadow-2xs space-y-4">
            <h2 className="text-base font-bold text-slate-900 border-b border-slate-100 pb-4">
              Case Information
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 text-xs">
              <div className="p-4 bg-slate-50 rounded-xl">
                <span className="text-slate-400 font-medium block mb-1">Reference Code</span>
                <span className="font-bold text-slate-800 text-sm">{caseData?.referenceCode || id}</span>
              </div>
              <div className="p-4 bg-slate-50 rounded-xl">
                <span className="text-slate-400 font-medium block mb-1">Client Name</span>
                <span className="font-bold text-slate-800 text-sm">{caseData?.clientOrganisationName || "Client"}</span>
              </div>
              <div className="p-4 bg-slate-50 rounded-xl">
                <span className="text-slate-400 font-medium block mb-1">Case Owner</span>
                <span className="font-bold text-slate-800 text-sm">{caseData?.caseOwnerName || "Maya Chen"}</span>
              </div>
              <div className="p-4 bg-slate-50 rounded-xl">
                <span className="text-slate-400 font-medium block mb-1">Current Status</span>
                <span className="font-bold text-blue-700 text-sm">{caseData?.status || "In progress"}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
}

export default function CaseDetailPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f4f7f9] flex items-center justify-center p-8 text-xs text-slate-500 font-medium">
          Loading Case Detail...
        </div>
      }
    >
      <CaseDetailContent />
    </Suspense>
  );
}
