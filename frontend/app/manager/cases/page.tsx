"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { StatusBadge } from "@/features/onboarding/components/StatusBadge";
import { OnboardingCaseSummary, CaseCounts } from "@/features/onboarding/types/onboarding.types";

export default function OnboardingCasesPage() {
  const router = useRouter();

  const [cases, setCases] = useState<OnboardingCaseSummary[]>([]);
  const [counts, setCounts] = useState<CaseCounts>({ all: 48, inProgress: 36, completed: 12 });
  const [activeTab, setActiveTab] = useState<"all" | "in_progress" | "completed">("all");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [ownerFilter, setOwnerFilter] = useState("Anyone");
  const [typeFilter, setTypeFilter] = useState("All");

  const [page, setPage] = useState(1);
  const [pageSize] = useState(5);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    let ignore = false;

    const loadCases = async () => {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        const params = new URLSearchParams();
        if (search.trim()) params.append("search", search.trim());

        if (activeTab === "in_progress") {
          params.append("status", "in progress");
        } else if (activeTab === "completed") {
          params.append("status", "completed");
        } else if (statusFilter !== "All") {
          params.append("status", statusFilter);
        }

        if (ownerFilter !== "Anyone") {
          params.append("owner", ownerFilter);
        }
        if (typeFilter !== "All") {
          params.append("type", typeFilter);
        }

        params.append("page", page.toString());
        params.append("pageSize", pageSize.toString());

        const res = await fetch(`${apiUrl}/api/onboarding/cases?${params.toString()}`, { headers });
        if (!ignore && res.ok) {
          const data = await res.json();
          setCases(data.items || []);
          setTotalCount(data.totalCount || 0);
          setTotalPages(data.totalPages || 1);
          if (data.counts) {
            setCounts(data.counts);
          }
        }
      } catch {
        // Fallback: continue showing current or empty state
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    };

    loadCases();
    return () => {
      ignore = true;
    };
  }, [apiUrl, search, activeTab, statusFilter, ownerFilter, typeFilter, page, pageSize]);

  const handleTabChange = (tab: "all" | "in_progress" | "completed") => {
    setLoading(true);
    setActiveTab(tab);
    setPage(1);
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      setLoading(true);
      setPage(newPage);
    }
  };

  return (
    <RoleGuard allowedRoles={["Administrator", "OnboardingManager"]}>
      <WorkspaceLayout breadcrumb="Onboarding cases">
        <div className="max-w-7xl mx-auto space-y-6">
          {/* Header Bar */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
                Onboarding cases
              </h1>
              <p className="text-sm text-slate-500 mt-1">
                Track every client, owner and outstanding requirement.
              </p>
            </div>

            <button
              type="button"
              onClick={() => router.push("/manager/cases/create")}
              className="inline-flex items-center justify-center gap-2 px-5 py-2.5 bg-[#0b7c74] hover:bg-[#096b64] text-white text-sm font-semibold rounded-xl shadow-xs transition-colors self-start sm:self-auto cursor-pointer"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              <span>Create case</span>
            </button>
          </div>

          {/* Status Tabs */}
          <div className="border-b border-slate-200">
            <nav className="flex space-x-8">
              <button
                type="button"
                onClick={() => handleTabChange("all")}
                className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
                  activeTab === "all"
                    ? "border-[#0b7c74] text-[#0b7c74] font-semibold"
                    : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                }`}
              >
                <span>All cases</span>
                <span className="text-xs text-slate-400 font-semibold">{counts.all}</span>
              </button>

              <button
                type="button"
                onClick={() => handleTabChange("in_progress")}
                className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
                  activeTab === "in_progress"
                    ? "border-[#0b7c74] text-[#0b7c74] font-semibold"
                    : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                }`}
              >
                <span>In progress</span>
                <span className="text-xs text-slate-400 font-semibold">{counts.inProgress}</span>
              </button>

              <button
                type="button"
                onClick={() => handleTabChange("completed")}
                className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
                  activeTab === "completed"
                    ? "border-[#0b7c74] text-[#0b7c74] font-semibold"
                    : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                }`}
              >
                <span>Completed</span>
                <span className="text-xs text-slate-400 font-semibold">{counts.completed}</span>
              </button>
            </nav>
          </div>

          {/* Filter Bar */}
          <div className="flex flex-wrap items-center gap-3">
            {/* Search Input */}
            <div className="relative flex-1 min-w-[240px] max-w-sm">
              <input
                type="text"
                placeholder="Search client name or case ID"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
                className="w-full pl-3.5 pr-4 py-2 text-xs bg-white border border-slate-200 rounded-xl text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] focus:border-transparent transition-all shadow-2xs"
              />
            </div>

            {/* Status Dropdown */}
            <div className="relative">
              <select
                value={statusFilter}
                onChange={(e) => {
                  setStatusFilter(e.target.value);
                  setPage(1);
                }}
                className="appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2 pr-8 text-xs text-slate-700 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] shadow-2xs cursor-pointer"
              >
                <option value="All">Status: All</option>
                <option value="In review">In review</option>
                <option value="Overdue">Overdue</option>
                <option value="Needs correction">Needs correction</option>
                <option value="Approved">Approved</option>
                <option value="Missing">Missing</option>
                <option value="Completed">Completed</option>
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>

            {/* Owner Dropdown */}
            <div className="relative">
              <select
                value={ownerFilter}
                onChange={(e) => {
                  setOwnerFilter(e.target.value);
                  setPage(1);
                }}
                className="appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2 pr-8 text-xs text-slate-700 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] shadow-2xs cursor-pointer"
              >
                <option value="Anyone">Owner: Anyone</option>
                <option value="Kasun">Kasun Jayawardena</option>
                <option value="Dimuka">Dimuka Perera</option>
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>

            {/* Type Dropdown */}
            <div className="relative">
              <select
                value={typeFilter}
                onChange={(e) => {
                  setTypeFilter(e.target.value);
                  setPage(1);
                }}
                className="appearance-none bg-white border border-slate-200 rounded-xl px-3.5 py-2 pr-8 text-xs text-slate-700 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] shadow-2xs cursor-pointer"
              >
                <option value="All">Type: All</option>
                <option value="Enterprise">Enterprise</option>
                <option value="SME">SME</option>
                <option value="Partner">Partner</option>
              </select>
              <div className="pointer-events-none absolute inset-y-0 right-2.5 flex items-center text-slate-400">
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-2xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/50">
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                      Client / Case
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                      Status
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                      Owner
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                      Progress
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase">
                      Due Date
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold tracking-wider text-slate-400 uppercase text-right">
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {loading ? (
                    <tr>
                      <td colSpan={6} className="py-12 text-center text-xs text-slate-400">
                        <div className="flex items-center justify-center gap-2">
                          <div className="w-4 h-4 border-2 border-[#0b7c74] border-t-transparent rounded-full animate-spin" />
                          <span>Loading cases...</span>
                        </div>
                      </td>
                    </tr>
                  ) : cases.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-12 text-center text-xs text-slate-400">
                        No onboarding cases found matching your criteria.
                      </td>
                    </tr>
                  ) : (
                    cases.map((item) => (
                      <tr
                        key={item.id}
                        className="hover:bg-slate-50/70 transition-colors group cursor-pointer"
                        onClick={() => router.push(`/manager/cases/${item.id}`)}
                      >
                        {/* Client / Case */}
                        <td className="py-4 px-6">
                          <div className="font-semibold text-slate-900 text-sm">
                            {item.clientOrganisationName}
                          </div>
                          <div className="text-xs text-slate-400 mt-0.5">
                            {item.referenceCode} · {item.clientTypeName}
                          </div>
                        </td>

                        {/* Status */}
                        <td className="py-4 px-6">
                          <StatusBadge status={item.status} />
                        </td>

                        {/* Owner */}
                        <td className="py-4 px-6 text-xs text-slate-700">
                          {item.ownerShortName || item.ownerName}
                        </td>

                        {/* Progress */}
                        <td className="py-4 px-6 text-xs text-slate-700">
                          {item.progressDisplay || `${item.approvedCount} / ${item.totalRequiredCount} approved`}
                        </td>

                        {/* Due Date */}
                        <td className="py-4 px-6 text-xs text-slate-700">
                          {item.dueDateDisplay}
                        </td>

                        {/* Open Action */}
                        <td className="py-4 px-6 text-right">
                          <Link
                            href={`/manager/cases/${item.id}`}
                            onClick={(e) => e.stopPropagation()}
                            className="inline-flex items-center gap-1 text-xs font-semibold text-[#0b7c74] hover:text-[#096b64] transition-colors"
                          >
                            <span>Open</span>
                            <span>&rarr;</span>
                          </Link>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination Footer */}
            <div className="py-3 px-6 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
              <div>
                Showing {totalCount > 0 ? (page - 1) * pageSize + 1 : 0}–{Math.min(page * pageSize, totalCount)} of {totalCount} cases
              </div>

              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => handlePageChange(page - 1)}
                  disabled={page <= 1}
                  className="px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-colors"
                >
                  Previous
                </button>

                {Array.from({ length: Math.min(totalPages, 5) }, (_, idx) => {
                  const pNum = idx + 1;
                  const isActive = pNum === page;
                  return (
                    <button
                      key={pNum}
                      type="button"
                      onClick={() => handlePageChange(pNum)}
                      className={`w-7 h-7 rounded-md text-xs font-semibold flex items-center justify-center transition-colors cursor-pointer ${
                        isActive
                          ? "bg-slate-900 text-white"
                          : "text-slate-600 hover:bg-slate-100"
                      }`}
                    >
                      {pNum}
                    </button>
                  );
                })}

                {totalPages > 5 && (
                  <>
                    <span className="px-1 text-slate-400">...</span>
                    <button
                      type="button"
                      onClick={() => handlePageChange(totalPages)}
                      className={`w-7 h-7 rounded-md text-xs font-semibold flex items-center justify-center transition-colors cursor-pointer ${
                        page === totalPages
                          ? "bg-slate-900 text-white"
                          : "text-slate-600 hover:bg-slate-100"
                      }`}
                    >
                      {totalPages}
                    </button>
                  </>
                )}

                <button
                  type="button"
                  onClick={() => handlePageChange(page + 1)}
                  disabled={page >= totalPages}
                  className="px-2.5 py-1 rounded-md text-xs font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition-colors"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}
