"use client";

import React, { Suspense, useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { StatusBadge } from "@/features/onboarding/components/StatusBadge";
import { OnboardingCaseDetail, OnboardingCaseItem } from "@/features/onboarding/types/onboarding.types";

function CaseDetailContent() {
  const router = useRouter();
  const params = useParams();
  const caseId = params?.id as string;

  const [caseData, setCaseData] = useState<OnboardingCaseDetail | null>(null);
  const [activeTab, setActiveTab] = useState<"documents" | "timeline" | "activity">("documents");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionNotice, setActionNotice] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    if (!caseId) return;

    let ignore = false;
    const fetchCase = async () => {
      try {
        setLoading(true);
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        const res = await fetch(`${apiUrl}/api/onboarding/cases/${caseId}`, { headers });
        if (res.ok) {
          const data: OnboardingCaseDetail = await res.json();
          if (!ignore) {
            setCaseData(data);
          }
        } else if (res.status === 404) {
          if (!ignore) setError("Onboarding case not found.");
        } else {
          if (!ignore) setError("Failed to load onboarding case.");
        }
      } catch (err: unknown) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Error connecting to service.");
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    };

    fetchCase();
    return () => {
      ignore = true;
    };
  }, [apiUrl, caseId]);

  const handleDocumentAction = (item: OnboardingCaseItem) => {
    if (item.status === "Approved" || item.status === "In review") {
      setActionNotice(`Viewing submitted document: ${item.uploadedFileName || item.documentName}`);
    } else if (item.status === "Needs correction") {
      setActionNotice(`Document replacement requested for: ${item.documentName}`);
    } else {
      setActionNotice(`Document upload dialog initialized for: ${item.documentName}`);
    }

    setTimeout(() => {
      setActionNotice(null);
    }, 4000);
  };

  const clientName = caseData?.clientOrganisation?.name || "Client Details";

  return (
    <RoleGuard allowedRoles={["Administrator", "OnboardingManager", "Reviewer", "Client"]}>
      <WorkspaceLayout breadcrumb={clientName}>
        <div className="max-w-6xl mx-auto space-y-6">
          {loading ? (
            <div className="py-24 text-center text-xs text-slate-400 flex items-center justify-center gap-2">
              <div className="w-5 h-5 border-2 border-[#0b7c74] border-t-transparent rounded-full animate-spin" />
              <span>Loading case details...</span>
            </div>
          ) : error || !caseData ? (
            <div className="bg-white rounded-2xl p-8 border border-slate-200 text-center">
              <p className="text-sm font-semibold text-slate-700">{error || "Case not found."}</p>
              <button
                type="button"
                onClick={() => router.push("/manager/cases")}
                className="mt-4 px-4 py-2 bg-[#0b7c74] text-white rounded-xl text-xs font-semibold cursor-pointer"
              >
                Return to onboarding cases
              </button>
            </div>
          ) : (
            <>
              {/* Top Case Title & Edit Action */}
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                  <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
                    {caseData.clientOrganisation?.name || "Ceylon Exports"}
                  </h1>
                  <p className="text-xs text-slate-400 mt-1">
                    {caseData.referenceCode} / {caseData.clientType?.name || "Enterprise"} onboarding
                  </p>
                </div>

                <button
                  type="button"
                  onClick={() => router.push(`/manager/cases/${caseData.id}/edit`)}
                  className="inline-flex items-center justify-center gap-2 px-5 py-2.5 bg-[#0b7c74] hover:bg-[#096b64] text-white text-xs font-semibold rounded-xl shadow-xs transition-colors self-start sm:self-auto cursor-pointer"
                >
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                  </svg>
                  <span>Edit case</span>
                </button>
              </div>

              {/* Progress Summary Card */}
              <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs p-6">
                <div className="flex items-center justify-between mb-3">
                  <div className="text-sm sm:text-base font-bold text-slate-900">
                    {caseData.progressDisplay || `${caseData.approvedCount} of ${caseData.totalRequiredCount} required items approved`}
                  </div>
                  <div>
                    <StatusBadge status={caseData.status} />
                  </div>
                </div>

                {/* Progress Bar */}
                <div className="w-full bg-slate-100 h-2 rounded-full overflow-hidden mb-3.5">
                  <div
                    className="bg-[#0b7c74] h-2 rounded-full transition-all duration-500"
                    style={{
                      width: `${Math.min(100, Math.max(5, caseData.progressPercentage))}%`,
                    }}
                  />
                </div>

                {/* Metadata Line */}
                <div className="text-xs text-slate-500 font-normal">
                  Owner: <span className="text-slate-700 font-medium">{caseData.owner?.fullName || "Kasun Jayawardena"}</span>
                  {" · "}
                  Due: <span className="text-slate-700 font-medium">{caseData.dueDateDisplay || "22 Oct 2026"}</span>
                  {" · "}
                  Client contact: <span className="text-slate-700 font-medium">{caseData.clientOrganisation?.contactName || "Nimali Perera"}</span>
                </div>
              </div>

              {/* Sub-tabs Strip */}
              <div className="border-b border-slate-200">
                <nav className="flex space-x-8">
                  <button
                    type="button"
                    onClick={() => setActiveTab("documents")}
                    className={`py-2 px-1 text-xs font-semibold border-b-2 transition-colors cursor-pointer ${
                      activeTab === "documents"
                        ? "border-[#0b7c74] text-[#0b7c74]"
                        : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                    }`}
                  >
                    Documents
                  </button>

                  <button
                    type="button"
                    onClick={() => setActiveTab("timeline")}
                    className={`py-2 px-1 text-xs font-semibold border-b-2 transition-colors cursor-pointer ${
                      activeTab === "timeline"
                        ? "border-[#0b7c74] text-[#0b7c74]"
                        : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                    }`}
                  >
                    Progress timeline
                  </button>

                  <button
                    type="button"
                    onClick={() => setActiveTab("activity")}
                    className={`py-2 px-1 text-xs font-semibold border-b-2 transition-colors cursor-pointer ${
                      activeTab === "activity"
                        ? "border-[#0b7c74] text-[#0b7c74]"
                        : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                    }`}
                  >
                    Case activity
                  </button>
                </nav>
              </div>

              {/* Temporary Action Toast Notification */}
              {actionNotice && (
                <div className="p-3.5 rounded-xl bg-teal-50 border border-teal-200 text-xs text-teal-800 flex items-center justify-between">
                  <span>{actionNotice}</span>
                  <button
                    type="button"
                    onClick={() => setActionNotice(null)}
                    className="text-teal-600 hover:text-teal-900 font-bold"
                  >
                    &times;
                  </button>
                </div>
              )}

              {/* TAB 1: DOCUMENTS */}
              {activeTab === "documents" && (
                <div className="space-y-4">
                  {/* Attention Banner if applicable */}
                  {caseData.attentionBannerMessage && (
                    <div className="bg-[#fffbeb] border border-[#fef3c7] rounded-xl p-4 flex items-start gap-3">
                      <div className="text-amber-500 mt-0.5 shrink-0">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                      </div>
                      <div>
                        <div className="text-xs font-bold text-amber-900">
                          {caseData.attentionBannerMessage}
                        </div>
                        <div className="text-xs text-amber-800 mt-0.5">
                          {caseData.attentionBannerSubtitle}
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Documents Table */}
                  <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs overflow-hidden">
                    <div className="overflow-x-auto">
                      <table className="w-full text-left border-collapse">
                        <thead>
                          <tr className="border-b border-slate-100 bg-slate-50/50">
                            <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                              Document
                            </th>
                            <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                              Status
                            </th>
                            <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                              Reviewer
                            </th>
                            <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                              Due Date
                            </th>
                            <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase text-right">
                              Action
                            </th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                          {caseData.items.map((item) => {
                            const isApproved = item.status.toLowerCase() === "approved";
                            const isInReview = item.status.toLowerCase() === "in review" || item.status.toLowerCase() === "inreview";
                            const isCorrection = item.status.toLowerCase() === "needs correction" || item.status.toLowerCase() === "needscorrection";

                            let actionLabel = "Upload ->";
                            if (isApproved || isInReview) {
                              actionLabel = "View ->";
                            } else if (isCorrection) {
                              actionLabel = "Replace ->";
                            }

                            return (
                              <tr key={item.id} className="hover:bg-slate-50/60 transition-colors">
                                {/* Document info */}
                                <td className="py-4 px-6">
                                  <div className="font-semibold text-slate-900 text-sm">
                                    {item.documentName}
                                  </div>
                                  <div className="text-xs text-slate-400 mt-0.5">
                                    {item.isRequired ? "Required" : "Optional"}
                                    {" · "}
                                    {item.uploadedFileName || "Not uploaded"}
                                  </div>
                                </td>

                                {/* Status */}
                                <td className="py-4 px-6">
                                  <StatusBadge status={item.status} />
                                </td>

                                {/* Reviewer */}
                                <td className="py-4 px-6 text-xs text-slate-700">
                                  {item.reviewerName || "Legal"}
                                </td>

                                {/* Due Date */}
                                <td className="py-4 px-6 text-xs text-slate-700">
                                  {item.dueDateDisplay}
                                </td>

                                {/* Action */}
                                <td className="py-4 px-6 text-right">
                                  <button
                                    type="button"
                                    onClick={() => handleDocumentAction(item)}
                                    className="text-xs font-semibold text-[#0b7c74] hover:text-[#096b64] cursor-pointer transition-colors"
                                  >
                                    {actionLabel}
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              )}

              {/* TAB 2: PROGRESS TIMELINE */}
              {activeTab === "timeline" && (
                <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs p-6 sm:p-8">
                  <h3 className="text-sm font-bold text-slate-900 mb-6">Onboarding Progress Timeline</h3>
                  <div className="relative pl-6 space-y-8 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200">
                    {caseData.timeline.map((event, idx) => {
                      const isComplete = event.status === "Completed";
                      const isCurr = event.status === "Current";

                      return (
                        <div key={idx} className="relative flex items-start gap-4">
                          <div
                            className={`absolute -left-6 top-1 w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold ${
                              isComplete
                                ? "bg-[#0b7c74] text-white"
                                : isCurr
                                ? "bg-amber-400 text-slate-900 ring-4 ring-amber-100"
                                : "bg-slate-200 text-slate-500"
                            }`}
                          >
                            {isComplete ? "✓" : idx + 1}
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center justify-between">
                              <h4 className="text-xs font-bold text-slate-900">{event.title}</h4>
                              <span className="text-[11px] text-slate-400 font-medium">{event.dateDisplay}</span>
                            </div>
                            <p className="text-xs text-slate-500 mt-0.5">{event.description}</p>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* TAB 3: CASE ACTIVITY */}
              {activeTab === "activity" && (
                <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs p-6 sm:p-8">
                  <h3 className="text-sm font-bold text-slate-900 mb-6">Case Audit Trail &amp; Activity</h3>
                  <div className="divide-y divide-slate-100">
                    {caseData.activities.map((act) => (
                      <div key={act.id} className="py-3.5 flex items-start justify-between gap-4">
                        <div>
                          <div className="text-xs font-semibold text-slate-800">
                            {act.details}
                          </div>
                          <div className="text-[11px] text-slate-400 mt-0.5">
                            By <span className="text-slate-600 font-medium">{act.actor}</span>
                          </div>
                        </div>
                        <div className="text-[11px] text-slate-400 whitespace-nowrap">
                          {act.dateDisplay}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}

export default function CaseDetailPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f3f6f7] flex items-center justify-center p-8 text-xs text-slate-500 font-medium">
          Loading Case Details...
        </div>
      }
    >
      <CaseDetailContent />
    </Suspense>
  );
}
