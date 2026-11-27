"use client";

import React, { Suspense, useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";

export interface CaseSummary {
  id: string;
  caseReference: string;
  clientOrganisationName: string;
  caseOwnerName: string;
  startDate: string;
  stage: string;
  status: string;
  ownerUserId?: string;
  clientOrganisationId?: string;
}

interface FilterOptions {
  stages: string[];
  owners: { id: string; fullName: string }[];
  statuses: string[];
}

const FIGMA_SAMPLE_CASES: CaseSummary[] = [
  {
    id: "case-0142",
    caseReference: "CASE-2026-0142",
    clientOrganisationName: "Aurelia Manufacturing Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-12T00:00:00Z",
    stage: "Document review",
    status: "In progress",
  },
  {
    id: "case-0143",
    caseReference: "CASE-2026-0143",
    clientOrganisationName: "Northstar Studio Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-13T00:00:00Z",
    stage: "Document collection",
    status: "In progress",
  },
  {
    id: "case-0144",
    caseReference: "CASE-2026-0144",
    clientOrganisationName: "Cedar Advisory Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-13T00:00:00Z",
    stage: "Document review",
    status: "In progress",
  },
  {
    id: "case-0145",
    caseReference: "CASE-2026-0145",
    clientOrganisationName: "Meridian Foods Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-14T00:00:00Z",
    stage: "Sequential approval",
    status: "In progress",
  },
  {
    id: "case-0146",
    caseReference: "CASE-2026-0146",
    clientOrganisationName: "Luma Design Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-15T00:00:00Z",
    stage: "Document collection",
    status: "In progress",
  },
  {
    id: "case-0138",
    caseReference: "CASE-2026-0138",
    clientOrganisationName: "Harbor Partners Ltd",
    caseOwnerName: "Maya Chen",
    startDate: "2026-10-05T00:00:00Z",
    stage: "Completed",
    status: "Complete",
  },
];

function formatDate(dateStr: string) {
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    return d.toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  } catch {
    return dateStr;
  }
}

function CasesContent() {
  const router = useRouter();

  const [cases, setCases] = useState<CaseSummary[]>(FIGMA_SAMPLE_CASES);
  const [filterOptions, setFilterOptions] = useState<FilterOptions>({
    stages: ["Document collection", "Document review", "Sequential approval", "Completed"],
    owners: [
      { id: "maya", fullName: "Maya Chen" },
      { id: "kasun", fullName: "Kasun Jayawardena" },
      { id: "dimuka", fullName: "Dimuka Perera" },
    ],
    statuses: ["In progress", "Complete"],
  });

  const [search, setSearch] = useState("");
  const [stageFilter, setStageFilter] = useState("All stages");
  const [ownerFilter, setOwnerFilter] = useState("All owners");
  const [statusFilter, setStatusFilter] = useState("All statuses");
  const [loading, setLoading] = useState(false);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  // Fetch from backend API
  useEffect(() => {
    let isCancelled = false;

    async function loadData() {
      try {
        setLoading(true);
        const token = typeof window !== "undefined" ? localStorage.getItem("onboardflow_token") : null;
        const headers: HeadersInit = {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        };

        const params = new URLSearchParams();
        if (search.trim()) params.append("search", search.trim());
        if (stageFilter !== "All stages") params.append("stage", stageFilter);
        if (ownerFilter !== "All owners") params.append("ownerId", ownerFilter);
        if (statusFilter !== "All statuses") params.append("status", statusFilter);

        const [casesRes, filtersRes] = await Promise.all([
          fetch(`${apiUrl}/api/cases?${params.toString()}`, { headers }).catch(() => null),
          fetch(`${apiUrl}/api/cases/filters`, { headers }).catch(() => null),
        ]);

        if (!isCancelled && casesRes && casesRes.ok) {
          const data: CaseSummary[] = await casesRes.json();
          if (Array.isArray(data) && data.length > 0) {
            setCases(data);
          } else if (Array.isArray(data) && data.length === 0 && (search.trim() || stageFilter !== "All stages" || ownerFilter !== "All owners" || statusFilter !== "All statuses")) {
            setCases([]);
          }
        }

        if (!isCancelled && filtersRes && filtersRes.ok) {
          const filterData: FilterOptions = await filtersRes.json();
          if (filterData) {
            setFilterOptions(filterData);
          }
        }
      } catch (err) {
        console.warn("Using sample case register data:", err);
      } finally {
        if (!isCancelled) setLoading(false);
      }
    }

    const timer = setTimeout(loadData, 200);
    return () => {
      isCancelled = true;
      clearTimeout(timer);
    };
  }, [apiUrl, search, stageFilter, ownerFilter, statusFilter]);

  // Client-side fallback filter if offline
  const filteredCases = useMemo(() => {
    return cases.filter((c) => {
      // Search term
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        const refMatch = c.caseReference.toLowerCase().includes(q);
        const orgMatch = c.clientOrganisationName.toLowerCase().includes(q);
        if (!refMatch && !orgMatch) return false;
      }

      // Stage
      if (stageFilter !== "All stages") {
        if (c.stage.toLowerCase() !== stageFilter.toLowerCase()) return false;
      }

      // Owner
      if (ownerFilter !== "All owners") {
        const ownerMatch =
          c.ownerUserId === ownerFilter ||
          c.caseOwnerName.toLowerCase().includes(ownerFilter.toLowerCase());
        if (!ownerMatch) return false;
      }

      // Status
      if (statusFilter !== "All statuses") {
        if (statusFilter === "In progress") {
          if (c.status.toLowerCase() !== "in progress" && c.status.toLowerCase() !== "in review") {
            return false;
          }
        } else if (statusFilter === "Complete") {
          if (c.status.toLowerCase() !== "complete" && c.status.toLowerCase() !== "completed") {
            return false;
          }
        }
      }

      return true;
    });
  }, [cases, search, stageFilter, ownerFilter, statusFilter]);

  const firstCaseId = filteredCases[0]?.id || "case-0142";
  const firstCaseRef = filteredCases[0]?.caseReference || "CASE-2026-0142";

  return (
    <WorkspaceLayout>
      <div className="flex-1 bg-[#f4f7f9] min-w-0 p-8 text-slate-800">
        <div className="max-w-6xl mx-auto space-y-6">
          {/* Top Breadcrumb & User Meta Bar */}
          <div className="flex items-center justify-between text-xs text-slate-500 pb-2 border-b border-slate-200">
            <div className="flex items-center gap-1.5 font-medium">
              <span className="text-slate-500">Onboarding Cases</span>
              <span>/</span>
              <span className="text-slate-700 font-semibold">Onboarding Cases</span>
            </div>
            <div className="text-slate-400 font-medium">
              Sample data · 19 Oct 2026 / <span className="text-slate-600 font-semibold">Maya Chen</span>
            </div>
          </div>

          {/* Page Header */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-1">
            <div>
              <div className="text-[11px] font-bold tracking-wider text-blue-700 uppercase mb-1">
                ONBOARDING MANAGER WORKSPACE
              </div>
              <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">
                Onboarding Cases
              </h1>
              <p className="text-xs text-slate-500 mt-1 font-medium">
                Search cases, track document work and keep onboarding moving.
              </p>
            </div>

            {/* + Create case Button */}
            {/* + Create case Button */}
            <div>
              <button
                type="button"
                onClick={() => router.push("/manager/cases/create")}
                className="px-5 py-2.5 bg-[#1d58d8] hover:bg-[#1a4dc0] active:bg-[#163fa1] text-white text-xs font-semibold rounded-lg shadow-sm transition flex items-center gap-1.5 cursor-pointer"
              >
                <span className="text-sm font-bold leading-none">+</span>
                <span>Create case</span>
              </button>
            </div>
          </div>

          {/* Search & Filters Row */}
          <div className="flex flex-col lg:flex-row items-stretch lg:items-center gap-3 pt-1">
            {/* Search Input (US-10) */}
            <div className="relative flex-1">
              <div className="pointer-events-none absolute inset-y-0 left-0 pl-3.5 flex items-center text-slate-400">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" strokeWidth={2} viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search Case ID or client"
                className="w-full pl-10 pr-4 py-2.5 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition shadow-2xs placeholder-slate-400"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => setSearch("")}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600"
                >
                  ✕
                </button>
              )}
            </div>

            {/* Filter: All stages */}
            <div className="relative w-full sm:w-44">
              <select
                value={stageFilter}
                onChange={(e) => setStageFilter(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs text-slate-700 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-8 cursor-pointer shadow-2xs font-medium"
              >
                <option value="All stages">All stages</option>
                {filterOptions.stages.map((st) => (
                  <option key={st} value={st}>
                    {st}
                  </option>
                ))}
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2.5 text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>

            {/* Filter: All owners */}
            <div className="relative w-full sm:w-44">
              <select
                value={ownerFilter}
                onChange={(e) => setOwnerFilter(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs text-slate-700 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-8 cursor-pointer shadow-2xs font-medium"
              >
                <option value="All owners">All owners</option>
                {filterOptions.owners.map((ow) => (
                  <option key={ow.id} value={ow.fullName}>
                    {ow.fullName}
                  </option>
                ))}
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2.5 text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>

            {/* Filter: All statuses */}
            <div className="relative w-full sm:w-40">
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs text-slate-700 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-8 cursor-pointer shadow-2xs font-medium"
              >
                <option value="All statuses">All statuses</option>
                {filterOptions.statuses.map((st) => (
                  <option key={st} value={st}>
                    {st}
                  </option>
                ))}
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2.5 text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>
          </div>

          {/* ---------------- Case Register Card ---------------- */}
          <div className="bg-white rounded-2xl border border-slate-200/90 shadow-2xs overflow-hidden">
            {/* Card Header */}
            <div className="p-6 border-b border-slate-100">
              <h2 className="text-base font-bold text-slate-900">Case register</h2>
              <p className="text-xs text-slate-400 mt-0.5">
                {filteredCases.length} sample cases · Select a case to view its checklist, documents and timeline
              </p>
            </div>

            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 text-[11px] font-bold text-slate-500 uppercase tracking-wider bg-slate-50/50">
                    <th className="py-3 px-6">Case / client</th>
                    <th className="py-3 px-6">Case owner</th>
                    <th className="py-3 px-6">Start date</th>
                    <th className="py-3 px-6">Stage</th>
                    <th className="py-3 px-6">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-xs">
                  {loading ? (
                    <tr>
                      <td colSpan={5} className="py-12 text-center text-slate-400 font-medium">
                        Loading cases...
                      </td>
                    </tr>
                  ) : filteredCases.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-16 text-center text-slate-400">
                        <div className="w-12 h-12 rounded-full bg-slate-50 flex items-center justify-center mx-auto mb-3 text-slate-300">
                          <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth={1.5} viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                          </svg>
                        </div>
                        <div className="font-semibold text-slate-700 text-sm">No matching cases found</div>
                        <p className="text-xs text-slate-400 mt-1 max-w-sm mx-auto">
                          Try adjusting your search query or reset the stage, owner, and status filters.
                        </p>
                      </td>
                    </tr>
                  ) : (
                    filteredCases.map((c) => {
                      const isComplete =
                        c.status.toLowerCase() === "complete" ||
                        c.status.toLowerCase() === "completed";

                      return (
                        <tr
                          key={c.id}
                          onClick={() => router.push(`/cases/${c.id}`)}
                          className="hover:bg-slate-50/80 transition cursor-pointer group"
                        >
                          {/* Case / Client */}
                          <td className="py-4 px-6">
                            <Link
                              href={`/cases/${c.id}`}
                              className="font-bold text-slate-900 group-hover:text-blue-700 transition"
                            >
                              {c.caseReference}
                            </Link>
                            <div className="text-slate-500 text-[11px] mt-0.5">
                              {c.clientOrganisationName}
                            </div>
                          </td>

                          {/* Case Owner */}
                          <td className="py-4 px-6 text-slate-700 font-medium">
                            {c.caseOwnerName}
                          </td>

                          {/* Start Date */}
                          <td className="py-4 px-6 text-slate-600">
                            {formatDate(c.startDate)}
                          </td>

                          {/* Stage */}
                          <td className="py-4 px-6 text-slate-600">
                            {c.stage}
                          </td>

                          {/* Status Badge */}
                          <td className="py-4 px-6">
                            {isComplete ? (
                              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200/80 shadow-2xs">
                                <svg className="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" strokeWidth={2.5} viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                                </svg>
                                <span>Complete</span>
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[11px] font-semibold bg-blue-50 text-blue-700 border border-blue-200/80 shadow-2xs">
                                <span className="w-1.5 h-1.5 rounded-full bg-blue-600 animate-pulse"></span>
                                <span>In progress</span>
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* Card Footer */}
            <div className="p-4 px-6 bg-slate-50/60 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
              <div>
                Showing {filteredCases.length} of {cases.length} illustrative records
              </div>
              <div>
                <Link
                  href={`/cases/${firstCaseId}`}
                  className="font-semibold text-blue-700 hover:text-blue-900 inline-flex items-center gap-1 group transition"
                >
                  <span>Open {firstCaseRef}</span>
                  <span className="group-hover:translate-x-0.5 transition-transform">→</span>
                </Link>
              </div>
            </div>
          </div>

          {/* ---------------- Sequential Approval & Workflow Bottom Info Cards ---------------- */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-1">
            {/* Left Card: Automatic completion */}
            <div className="bg-[#eff6ff] border border-blue-200/80 rounded-2xl p-5">
              <div className="text-xs font-bold text-blue-900">
                Automatic completion
              </div>
              <p className="text-xs text-blue-800/80 mt-1 leading-relaxed font-normal">
                A case completes when all required documents are approved. Optional documents do not block completion. There is no manual Complete action.
              </p>
            </div>

            {/* Right Card: Illustrative workflow */}
            <div className="bg-[#eff6ff] border border-blue-200/80 rounded-2xl p-5">
              <div className="text-xs font-bold text-blue-900">
                Illustrative workflow
              </div>
              <p className="text-xs text-blue-800/80 mt-1 leading-relaxed font-normal">
                Stage and status vocabulary, calculation methods and final report definitions: Pending confirmation
              </p>
            </div>
          </div>

          {/* Bottom Portal Footer */}
          <div className="text-[11px] text-slate-400 pt-2 pb-6">
            ACCORD / PORTAL · Illustrative records only · Workflow status vocabulary Pending confirmation
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
}

export default function CasesPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f4f7f9] flex items-center justify-center p-8 text-xs text-slate-500 font-medium">
          Loading Onboarding Cases...
        </div>
      }
    >
      <CasesContent />
    </Suspense>
  );
}
