"use client";

import React, { useEffect, useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { CaseFormOptions, OnboardingCaseDetail } from "../types/onboarding.types";

interface OnboardingCaseFormProps {
  mode: "create" | "edit";
  caseId?: string;
}

export function OnboardingCaseForm({ mode, caseId }: OnboardingCaseFormProps) {
  const router = useRouter();
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const isEdit = mode === "edit";

  // Form states
  const [organisations, setOrganisations] = useState<CaseFormOptions["organisations"]>([]);
  const [clientTypes, setClientTypes] = useState<CaseFormOptions["clientTypes"]>([]);
  const [owners, setOwners] = useState<CaseFormOptions["owners"]>([]);

  const [selectedOrgId, setSelectedOrgId] = useState("");
  const [selectedTypeId, setSelectedTypeId] = useState("");
  const [selectedOwnerId, setSelectedOwnerId] = useState("");
  const [targetDate, setTargetDate] = useState("2026-10-22");
  const [status, setStatus] = useState("In review");

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch form options (organisations, client types, owners) and existing case if edit mode
  useEffect(() => {
    let ignore = false;

    const loadData = async () => {
      try {
        setLoading(true);
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        const optionsRes = await fetch(`${apiUrl}/api/onboarding/cases/form-options`, { headers });
        let optionsData: CaseFormOptions | null = null;
        if (optionsRes.ok) {
          optionsData = await optionsRes.json();
          if (!ignore && optionsData) {
            setOrganisations(optionsData.organisations || []);
            setClientTypes(optionsData.clientTypes || []);
            setOwners(optionsData.owners || []);
          }
        }

        if (isEdit && caseId) {
          const caseRes = await fetch(`${apiUrl}/api/onboarding/cases/${caseId}`, { headers });
          if (caseRes.ok) {
            const caseData: OnboardingCaseDetail = await caseRes.json();
            if (!ignore && caseData) {
              setSelectedOrgId(caseData.clientOrganisation.id);
              setSelectedTypeId(caseData.clientType.id);
              setSelectedOwnerId(caseData.owner.id);
              if (caseData.targetCompletionDate) {
                setTargetDate(caseData.targetCompletionDate.split("T")[0]);
              }
              setStatus(caseData.status || "In review");
            }
          }
        } else if (!ignore && optionsData) {
          // Defaults for create mode matching Figma
          if (optionsData.organisations.length > 0) {
            const ceylon = optionsData.organisations.find((o) =>
              o.name.toLowerCase().includes("ceylon")
            ) || optionsData.organisations[0];
            setSelectedOrgId(ceylon.id);

            // Auto-select client type matching category
            const matchedType = optionsData.clientTypes.find(
              (t) => t.name.toLowerCase() === ceylon.category.toLowerCase()
            ) || optionsData.clientTypes[0];
            if (matchedType) setSelectedTypeId(matchedType.id);
          }

          if (optionsData.owners.length > 0) {
            const kasun = optionsData.owners.find((u) =>
              u.fullName.toLowerCase().includes("kasun")
            ) || optionsData.owners[0];
            setSelectedOwnerId(kasun.id);
          }
        }
      } catch (err: unknown) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Failed to load form details.");
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    };

    loadData();
    return () => {
      ignore = true;
    };
  }, [apiUrl, isEdit, caseId]);

  // Handle client organisation selection change
  const handleOrgChange = (orgId: string) => {
    setSelectedOrgId(orgId);
    const org = organisations.find((o) => o.id === orgId);
    if (org) {
      const matchingType = clientTypes.find(
        (t) => t.name.toLowerCase() === org.category.toLowerCase()
      );
      if (matchingType) {
        setSelectedTypeId(matchingType.id);
      }
    }
  };

  // Resolve current active client type & template details
  const currentOrg = useMemo(() => {
    return organisations.find((o) => o.id === selectedOrgId);
  }, [organisations, selectedOrgId]);

  const currentType = useMemo(() => {
    return clientTypes.find((t) => t.id === selectedTypeId) || clientTypes[0];
  }, [clientTypes, selectedTypeId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!selectedOrgId) {
      setError("Please select a client organisation.");
      return;
    }
    if (!selectedOwnerId) {
      setError("Please select a case owner.");
      return;
    }
    if (!targetDate) {
      setError("Please choose a target completion date.");
      return;
    }

    try {
      setSubmitting(true);
      const token = localStorage.getItem("onboardflow_token");
      const headers: HeadersInit = {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      };

      if (isEdit && caseId) {
        const payload = {
          clientOrganisationId: selectedOrgId,
          clientTypeId: selectedTypeId,
          ownerUserId: selectedOwnerId,
          targetCompletionDate: targetDate,
          status: status,
        };

        const res = await fetch(`${apiUrl}/api/onboarding/cases/${caseId}`, {
          method: "PUT",
          headers,
          body: JSON.stringify(payload),
        });

        if (!res.ok) {
          const errData = await res.json().catch(() => null);
          throw new Error(errData?.message || "Failed to update onboarding case.");
        }

        router.push(`/manager/cases/${caseId}`);
      } else {
        const payload = {
          clientOrganisationId: selectedOrgId,
          clientTypeId: selectedTypeId,
          ownerUserId: selectedOwnerId,
          targetCompletionDate: targetDate,
          status: "In review",
        };

        const res = await fetch(`${apiUrl}/api/onboarding/cases`, {
          method: "POST",
          headers,
          body: JSON.stringify(payload),
        });

        if (!res.ok) {
          const errData = await res.json().catch(() => null);
          throw new Error(errData?.message || "Failed to create onboarding case.");
        }

        const createdCase = await res.json();
        router.push(`/manager/cases/${createdCase.id}`);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "An error occurred while saving the case.");
    } finally {
      setSubmitting(false);
    }
  };

  const pageTitle = isEdit ? "Edit onboarding case" : "Create onboarding case";
  const pageSubtitle = isEdit
    ? "Update case details and checklist requirements."
    : "Start a case using the checklist linked to the client type.";

  const previewItemsList = currentType?.previewItems && currentType.previewItems.length > 0
    ? currentType.previewItems
    : [
        "Business registration",
        "Signed agreement",
        "Tax certificate",
        "Bank confirmation",
        "Director ID",
        "Address proof",
      ];

  return (
    <RoleGuard allowedRoles={["Administrator", "OnboardingManager"]}>
      <WorkspaceLayout breadcrumb={pageTitle}>
        <div className="max-w-5xl mx-auto space-y-6">
          {/* Header */}
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
              {pageTitle}
            </h1>
            <p className="text-sm text-slate-500 mt-1">{pageSubtitle}</p>
          </div>

          {/* 3-Step Indicator Strip */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {/* Step 01 */}
            <div className="bg-white rounded-xl p-4 border-2 border-[#0b7c74] shadow-2xs flex flex-col justify-between">
              <span className="text-xs font-bold text-[#0b7c74] tracking-wider">01</span>
              <span className="text-sm font-bold text-slate-900 mt-1">Client &amp; ownership</span>
            </div>

            {/* Step 02 */}
            <div className="bg-white rounded-xl p-4 border border-slate-200/80 shadow-2xs flex flex-col justify-between opacity-80">
              <span className="text-xs font-semibold text-slate-400 tracking-wider">02</span>
              <span className="text-sm font-medium text-slate-600 mt-1">Checklist preview</span>
            </div>

            {/* Step 03 */}
            <div className="bg-white rounded-xl p-4 border border-slate-200/80 shadow-2xs flex flex-col justify-between opacity-80">
              <span className="text-xs font-semibold text-slate-400 tracking-wider">03</span>
              <span className="text-sm font-medium text-slate-600 mt-1">Create &amp; notify</span>
            </div>
          </div>

          {/* Form Card */}
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs p-6 sm:p-8">
            <h2 className="text-base font-bold text-slate-900 mb-6">Client &amp; ownership</h2>

            {error && (
              <div className="mb-6 p-4 rounded-xl bg-red-50 border border-red-200 text-xs text-red-700">
                {error}
              </div>
            )}

            {loading ? (
              <div className="py-16 text-center text-xs text-slate-400 flex items-center justify-center gap-2">
                <div className="w-4 h-4 border-2 border-[#0b7c74] border-t-transparent rounded-full animate-spin" />
                <span>Loading options...</span>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                  {/* Client Organisation */}
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Client organisation <span className="text-red-500">*</span>
                    </label>
                    <div className="relative">
                      <select
                        value={selectedOrgId}
                        onChange={(e) => handleOrgChange(e.target.value)}
                        required
                        className="w-full appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2.5 pr-8 text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] focus:border-transparent transition-all shadow-2xs cursor-pointer"
                      >
                        {organisations.map((org) => (
                          <option key={org.id} value={org.id}>
                            {org.name}
                          </option>
                        ))}
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                      </div>
                    </div>
                  </div>

                  {/* Client Type */}
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Client type
                    </label>
                    <div className="relative">
                      <input
                        type="text"
                        readOnly
                        value={`${currentType?.name || currentOrg?.category || "Enterprise"} · From organisation`}
                        className="w-full bg-slate-50 border border-slate-200 rounded-xl px-3.5 py-2.5 text-xs text-slate-700 select-none shadow-2xs cursor-default"
                      />
                    </div>
                  </div>

                  {/* Case Owner */}
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Case owner <span className="text-red-500">*</span>
                    </label>
                    <div className="relative">
                      <select
                        value={selectedOwnerId}
                        onChange={(e) => setSelectedOwnerId(e.target.value)}
                        required
                        className="w-full appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2.5 pr-8 text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] focus:border-transparent transition-all shadow-2xs cursor-pointer"
                      >
                        {owners.map((owner) => (
                          <option key={owner.id} value={owner.id}>
                            {owner.fullName}
                          </option>
                        ))}
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                      </div>
                    </div>
                  </div>

                  {/* Target Completion Date */}
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Target completion date <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="date"
                      value={targetDate}
                      onChange={(e) => setTargetDate(e.target.value)}
                      required
                      className="w-full bg-white border border-slate-200 rounded-xl px-3.5 py-2 text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] focus:border-transparent transition-all shadow-2xs"
                    />
                  </div>
                </div>

                {/* Edit status dropdown if editing */}
                {isEdit && (
                  <div className="max-w-xs">
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Case status
                    </label>
                    <div className="relative">
                      <select
                        value={status}
                        onChange={(e) => setStatus(e.target.value)}
                        className="w-full appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2 pr-8 text-xs text-slate-800 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] shadow-2xs cursor-pointer"
                      >
                        <option value="In review">In review</option>
                        <option value="Overdue">Overdue</option>
                        <option value="Needs correction">Needs correction</option>
                        <option value="Approved">Approved</option>
                        <option value="Missing">Missing</option>
                        <option value="Completed">Completed</option>
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                      </div>
                    </div>
                  </div>
                )}

                {/* Teal Callout Banner */}
                <div className="bg-[#e6f4f2] border border-[#b2ded9] rounded-xl p-4 transition-all">
                  <div className="font-bold text-xs text-[#0b7c74]">
                    {currentType?.defaultChecklistTemplateName || "Enterprise onboarding"} · {currentType?.defaultChecklistTemplateVersion || "version 3"}
                  </div>
                  <p className="text-xs text-[#14534f] mt-0.5">
                    {currentType?.requiredCount || 6} required documents and {currentType?.optionalCount || 2} optional items will be added automatically.
                  </p>
                </div>

                {/* Checklist Preview */}
                <div className="pt-2">
                  <h3 className="text-xs font-bold text-slate-900 uppercase tracking-wider mb-2">
                    Checklist preview
                  </h3>
                  <div className="text-xs text-slate-600 leading-relaxed font-normal">
                    {previewItemsList.join(" · ")}
                  </div>
                </div>

                {/* Actions */}
                <div className="pt-4 flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => {
                      if (isEdit && caseId) {
                        router.push(`/manager/cases/${caseId}`);
                      } else {
                        router.push("/manager/cases");
                      }
                    }}
                    className="px-5 py-2.5 rounded-xl border border-slate-200 text-xs font-semibold text-slate-700 hover:bg-slate-50 transition-colors cursor-pointer"
                  >
                    Cancel
                  </button>

                  <button
                    type="submit"
                    disabled={submitting}
                    className="px-6 py-2.5 bg-[#0b7c74] hover:bg-[#096b64] text-white text-xs font-semibold rounded-xl shadow-xs transition-colors disabled:opacity-50 cursor-pointer flex items-center gap-2"
                  >
                    {submitting && (
                      <div className="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    )}
                    <span>{isEdit ? "Save changes" : "Create case"}</span>
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}
