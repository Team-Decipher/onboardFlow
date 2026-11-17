"use client";

import React, { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";

interface ChecklistTemplate {
  id: string;
  name: string;
  version: string;
  displayName?: string;
  description?: string;
  associatedClientTypeId?: string | null;
  associatedClientTypeName?: string | null;
  requiredCount: number;
  optionalCount: number;
  createdAtUtc?: string;
}

interface ClientTypeOption {
  id: string;
  name: string;
  description?: string;
}

const DEFAULT_CLIENT_TYPES: ClientTypeOption[] = [
  { id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Enterprise" },
  { id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "SME" },
  { id: "cccccccc-cccc-cccc-cccc-cccccccccccc", name: "Partner" },
];

const DEFAULT_TEMPLATES: ChecklistTemplate[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Enterprise onboarding",
    version: "v3",
    description: "Complete enterprise compliance, financial verification and multi-stage executive signoff.",
    associatedClientTypeId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    associatedClientTypeName: "Enterprise",
    requiredCount: 6,
    optionalCount: 2,
    createdAtUtc: "2026-01-01T00:00:00Z",
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    name: "SME onboarding",
    version: "v2",
    description: "A streamlined verification pack designed for small and medium tier operations.",
    associatedClientTypeId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    associatedClientTypeName: "SME",
    requiredCount: 4,
    optionalCount: 1,
    createdAtUtc: "2026-01-01T00:00:00Z",
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    name: "Partner onboarding",
    version: "v1",
    description: "Partner governance agreements and channel accreditation verification.",
    associatedClientTypeId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    associatedClientTypeName: "Partner",
    requiredCount: 5,
    optionalCount: 1,
    createdAtUtc: "2026-01-01T00:00:00Z",
  },
];

export default function ChecklistsPage() {
  const router = useRouter();
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const [templates, setTemplates] = useState<ChecklistTemplate[]>(DEFAULT_TEMPLATES);
  const [clientTypes, setClientTypes] = useState<ClientTypeOption[]>(DEFAULT_CLIENT_TYPES);
  const [selectedClientTypeFilter, setSelectedClientTypeFilter] = useState<string>("ALL");
  const [searchQuery, setSearchQuery] = useState("");
  const [loading, setLoading] = useState(false);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formName, setFormName] = useState("");
  const [formClientTypeId, setFormClientTypeId] = useState("");
  const [formVersion, setFormVersion] = useState("v1");
  const [formDescription, setFormDescription] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Fetch Templates and Client Types
  useEffect(() => {
    let ignore = false;

    async function loadData() {
      setLoading(true);
      try {
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        const [tplRes, typesRes] = await Promise.all([
          fetch(`${apiUrl}/api/checklist-templates`, { headers }),
          fetch(`${apiUrl}/api/client-types`, { headers }),
        ]);

        if (ignore) return;

        if (typesRes.ok) {
          const typesData: ClientTypeOption[] = await typesRes.json();
          if (typesData.length > 0) {
            setClientTypes(typesData);
            setFormClientTypeId((prev) => prev || typesData[0].id);
          }
        }

        if (tplRes.ok) {
          const tplData: ChecklistTemplate[] = await tplRes.json();
          if (tplData.length > 0) {
            setTemplates(tplData);
          }
        }
      } catch (err) {
        console.warn("Could not reach backend API; fallback to local data", err);
      } finally {
        if (!ignore) setLoading(false);
      }
    }

    void loadData();

    return () => {
      ignore = true;
    };
  }, [apiUrl]);

  // Filtered Templates
  const filteredTemplates = useMemo(() => {
    return templates.filter((tpl) => {
      const matchesType =
        selectedClientTypeFilter === "ALL" ||
        tpl.associatedClientTypeId === selectedClientTypeFilter ||
        tpl.associatedClientTypeName?.toLowerCase() === selectedClientTypeFilter.toLowerCase();

      const matchesSearch =
        !searchQuery.trim() ||
        tpl.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        tpl.description?.toLowerCase().includes(searchQuery.toLowerCase());

      return matchesType && matchesSearch;
    });
  }, [templates, selectedClientTypeFilter, searchQuery]);

  // Open Modal Handler
  const handleOpenModal = () => {
    setFormName("");
    setFormVersion("v1");
    setFormDescription("");
    setFormClientTypeId(clientTypes[0]?.id || "");
    setFormError(null);
    setIsModalOpen(true);
  };

  // Create Template Submit Handler
  const handleCreateTemplate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    const trimmedName = formName.trim();
    if (!trimmedName || trimmedName.length < 3 || trimmedName.length > 150) {
      setFormError("Template name must be between 3 and 150 characters.");
      return;
    }

    if (!formClientTypeId) {
      setFormError("Please select a client type.");
      return;
    }

    setSubmitting(true);
    try {
      const token = localStorage.getItem("onboardflow_token");
      const headers: HeadersInit = {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      };

      const payload = {
        name: trimmedName,
        clientTypeId: formClientTypeId,
        version: formVersion.trim() || "v1",
        description: formDescription.trim() || undefined,
      };

      const res = await fetch(`${apiUrl}/api/checklist-templates`, {
        method: "POST",
        headers,
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        let errorDetail = "Failed to create checklist template.";
        try {
          const problem = await res.json();
          errorDetail = problem.detail || problem.message || errorDetail;
        } catch {
          // ignore parsing error
        }
        setFormError(errorDetail);
        setSubmitting(false);
        return;
      }

      const created = await res.json();
      setIsModalOpen(false);

      // Navigate directly to builder for the new template
      router.push(`/admin/checklist-templates/${created.id}`);
    } catch (err) {
      console.warn("API request failed; simulating local template creation", err);
      // Fallback for offline simulation
      const targetType = clientTypes.find((c) => c.id === formClientTypeId);
      const newTemplate: ChecklistTemplate = {
        id: `tpl-${Date.now()}`,
        name: trimmedName,
        version: formVersion.trim() || "v1",
        description: formDescription.trim(),
        associatedClientTypeId: formClientTypeId,
        associatedClientTypeName: targetType?.name || "Custom",
        requiredCount: 0,
        optionalCount: 0,
        createdAtUtc: new Date().toISOString(),
      };
      setTemplates((prev) => [newTemplate, ...prev]);
      setIsModalOpen(false);
      router.push(`/admin/checklist-templates/${newTemplate.id}`);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <WorkspaceLayout>
      <div className="p-8 max-w-7xl mx-auto space-y-6">
        {/* Top Header & Breadcrumb */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-slate-200">
          <div>
            <div className="flex items-center gap-2 text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
              <span>Admin</span>
              <span>/</span>
              <span className="text-cyan-700 font-bold">Checklist Templates</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">
              Checklist Templates
            </h1>
            <p className="text-sm text-slate-600 mt-1">
              Define document submission requirements and default review policies for each client tier.
            </p>
          </div>

          <button
            onClick={handleOpenModal}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg bg-[#0d272d] text-white text-sm font-semibold hover:bg-[#143d46] transition-all shadow-sm shrink-0 cursor-pointer"
          >
            <svg
              className="w-4 h-4 text-cyan-400"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Create Checklist Template
          </button>
        </div>

        {/* Filter and Search Controls */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-white p-4 rounded-xl border border-slate-200/80 shadow-xs">
          {/* Client Type Filter Tabs */}
          <div className="flex items-center gap-1.5 overflow-x-auto w-full sm:w-auto pb-1 sm:pb-0">
            <button
              onClick={() => setSelectedClientTypeFilter("ALL")}
              className={`px-3.5 py-1.5 text-xs font-semibold rounded-lg transition-colors cursor-pointer ${
                selectedClientTypeFilter === "ALL"
                  ? "bg-[#0d272d] text-cyan-300"
                  : "bg-slate-100 text-slate-600 hover:bg-slate-200"
              }`}
            >
              All Types
            </button>
            {clientTypes.map((type) => (
              <button
                key={type.id}
                onClick={() => setSelectedClientTypeFilter(type.id)}
                className={`px-3.5 py-1.5 text-xs font-semibold rounded-lg transition-colors cursor-pointer ${
                  selectedClientTypeFilter === type.id
                    ? "bg-[#0d272d] text-cyan-300"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                {type.name}
              </button>
            ))}
          </div>

          {/* Search Box */}
          <div className="relative w-full sm:w-64">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search templates..."
              className="w-full text-xs px-3.5 py-2 pl-9 bg-slate-50 border border-slate-300 rounded-lg text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:bg-white"
            />
            <svg
              className="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
              />
            </svg>
          </div>
        </div>

        {/* Template Cards Grid */}
        {loading ? (
          <div className="p-12 text-center text-slate-500 text-sm">
            Loading checklist templates...
          </div>
        ) : filteredTemplates.length === 0 ? (
          <div className="bg-white rounded-xl border border-dashed border-slate-300 p-12 text-center">
            <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center mx-auto mb-3 text-slate-400">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h3 className="text-sm font-semibold text-slate-800">No checklist templates found</h3>
            <p className="text-xs text-slate-500 mt-1">
              {searchQuery ? "Try refining your search query." : "Get started by creating your first document checklist template."}
            </p>
            <button
              onClick={handleOpenModal}
              className="mt-4 inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-md bg-cyan-700 text-white hover:bg-cyan-800"
            >
              + Create Template
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredTemplates.map((template) => (
              <div
                key={template.id}
                className="bg-white rounded-xl border border-slate-200/90 hover:border-cyan-500/50 hover:shadow-md transition-all flex flex-col justify-between overflow-hidden group"
              >
                <div className="p-5">
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-cyan-50 text-cyan-800 border border-cyan-200">
                      {template.associatedClientTypeName || "Client Tier"}
                    </span>
                    <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-100 text-slate-600 font-medium">
                      {template.version}
                    </span>
                  </div>

                  <h3 className="text-base font-bold text-slate-900 group-hover:text-cyan-800 transition-colors">
                    {template.name}
                  </h3>

                  <p className="text-xs text-slate-500 mt-2 line-clamp-2 leading-relaxed min-h-[36px]">
                    {template.description || "No description provided."}
                  </p>

                  <div className="flex items-center gap-2 mt-4 pt-4 border-t border-slate-100 text-xs">
                    <span className="inline-flex items-center gap-1 font-medium text-slate-700">
                      <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
                      {template.requiredCount} Required
                    </span>
                    <span className="text-slate-300">•</span>
                    <span className="inline-flex items-center gap-1 font-medium text-slate-500">
                      <span className="w-2 h-2 rounded-full bg-amber-400"></span>
                      {template.optionalCount} Optional
                    </span>
                  </div>
                </div>

                <div className="px-5 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between">
                  <span className="text-[11px] text-slate-400 font-medium">
                    {template.requiredCount + template.optionalCount} items total
                  </span>
                  <Link
                    href={`/admin/checklist-templates/${template.id}`}
                    className="inline-flex items-center gap-1.5 text-xs font-semibold text-cyan-700 hover:text-cyan-900 transition-colors"
                  >
                    Open Builder
                    <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                    </svg>
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Create Checklist Template Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-950/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            {/* Modal Header */}
            <div className="px-6 py-5 bg-[#0d272d] text-white flex items-center justify-between">
              <div>
                <h2 className="text-base font-bold text-white">Create Checklist Template</h2>
                <p className="text-xs text-[#5e8b94] mt-0.5">
                  Define document checklist specifications for a client type tier.
                </p>
              </div>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded-md transition-colors cursor-pointer"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Modal Body */}
            <form onSubmit={handleCreateTemplate} className="p-6 space-y-4">
              {formError && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700 flex items-start gap-2">
                  <svg className="w-4 h-4 text-red-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                  </svg>
                  <span>{formError}</span>
                </div>
              )}

              {/* Client Type Select */}
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1.5">
                  Client Type Tier <span className="text-red-500">*</span>
                </label>
                <select
                  value={formClientTypeId}
                  onChange={(e) => setFormClientTypeId(e.target.value)}
                  required
                  className="w-full text-sm px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:bg-white"
                >
                  {clientTypes.map((ct) => (
                    <option key={ct.id} value={ct.id}>
                      {ct.name}
                    </option>
                  ))}
                </select>
                <p className="text-[11px] text-slate-500 mt-1">
                  The client tier that will use this document checklist by default.
                </p>
              </div>

              {/* Template Name */}
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1.5">
                  Template Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g. Enterprise Onboarding Checklist"
                  required
                  minLength={3}
                  maxLength={150}
                  className="w-full text-sm px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:bg-white placeholder-slate-400"
                />
                <div className="flex justify-between items-center text-[11px] text-slate-500 mt-1">
                  <span>Between 3 and 150 characters.</span>
                  <span>{formName.length}/150</span>
                </div>
              </div>

              {/* Version */}
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1.5">
                  Version
                </label>
                <input
                  type="text"
                  value={formVersion}
                  onChange={(e) => setFormVersion(e.target.value)}
                  placeholder="e.g. v1, v2"
                  maxLength={20}
                  className="w-full text-sm px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:bg-white"
                />
              </div>

              {/* Description */}
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 mb-1.5">
                  Description
                </label>
                <textarea
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="Describe the regulatory or compliance purpose of this checklist..."
                  rows={3}
                  maxLength={500}
                  className="w-full text-sm px-3.5 py-2 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:bg-white placeholder-slate-400"
                />
              </div>

              {/* Modal Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-200">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white bg-cyan-700 hover:bg-cyan-800 disabled:opacity-50 rounded-lg transition-colors shadow-xs cursor-pointer"
                >
                  {submitting ? (
                    <>
                      <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                      </svg>
                      Creating...
                    </>
                  ) : (
                    "Create Template"
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </WorkspaceLayout>
  );
}
