"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { UserProfile } from "@/features/auth/types/auth.types";

export default function UserAccountsPage() {
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("All");
  const [statusFilter, setStatusFilter] = useState("All");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    let ignore = false;

    async function loadUsers() {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const params = new URLSearchParams();
        if (search) params.append("search", search);
        if (roleFilter !== "All") params.append("role", roleFilter);
        if (statusFilter !== "All") params.append("status", statusFilter.toLowerCase());

        const res = await fetch(`${apiUrl}/api/users?${params.toString()}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!ignore) {
          if (res.ok) {
            const data = await res.json();
            setUsers(data);
          } else {
            const err = await res.json().catch(() => null);
            setErrorMessage(err?.message || "Failed to load user accounts.");
          }
          setIsLoading(false);
        }
      } catch {
        if (!ignore) {
          setErrorMessage("Could not connect to backend server.");
          setIsLoading(false);
        }
      }
    }

    loadUsers();

    return () => {
      ignore = true;
    };
  }, [apiUrl, search, roleFilter, statusFilter]);

  return (
    <RoleGuard allowedRoles={["Administrator"]}>
      <WorkspaceLayout>
        <div className="max-w-6xl mx-auto space-y-6">
          {/* Header */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                User accounts
              </h1>
              <p className="text-xs text-slate-500 mt-1">
                Give each person the access they need.
              </p>
            </div>

            <Link
              href="/admin/users/create"
              className="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#0b7c74] hover:bg-[#096963] text-white font-semibold text-xs transition-colors shadow-sm self-start sm:self-auto"
            >
              <span>+ Add user</span>
            </Link>
          </div>

          {/* Filters Bar */}
          <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col sm:flex-row items-center gap-3">
            <div className="flex-1 w-full relative">
              <input
                type="text"
                placeholder="Search name or email"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
              />
            </div>

            <div className="flex w-full sm:w-auto items-center gap-3">
              <select
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value)}
                className="px-3 py-2 rounded-xl border border-slate-200 text-xs text-slate-700 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
              >
                <option value="All">Role: All</option>
                <option value="Administrator">Administrator</option>
                <option value="OnboardingManager">Onboarding Manager</option>
                <option value="Reviewer">Reviewer</option>
                <option value="Client">Client</option>
              </select>

              <select
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
                className="px-3 py-2 rounded-xl border border-slate-200 text-xs text-slate-700 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
              >
                <option value="All">Status: All</option>
                <option value="Active">Active</option>
                <option value="Deactivated">Deactivated</option>
              </select>
            </div>
          </div>

          {/* Error Message */}
          {errorMessage && (
            <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs">
              {errorMessage}
            </div>
          )}

          {/* Users Table */}
          <div className="bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                    <th className="py-3 px-5">User</th>
                    <th className="py-3 px-5">Role</th>
                    <th className="py-3 px-5">Organisation</th>
                    <th className="py-3 px-5">Status</th>
                    <th className="py-3 px-5 text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
                  {isLoading ? (
                    <tr>
                      <td colSpan={5} className="py-10 text-center text-slate-400">
                        Loading users...
                      </td>
                    </tr>
                  ) : users.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-10 text-center text-slate-400">
                        No users found matching current filters.
                      </td>
                    </tr>
                  ) : (
                    users.map((u) => (
                      <tr key={u.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="py-3.5 px-5">
                          <div className="font-semibold text-slate-900">{u.fullName}</div>
                          <div className="text-[11px] text-slate-400">{u.email}</div>
                        </td>
                        <td className="py-3.5 px-5 font-medium text-slate-700">
                          {u.role}
                        </td>
                        <td className="py-3.5 px-5 text-slate-600">
                          {u.clientOrganisationName || "Internal team"}
                        </td>
                        <td className="py-3.5 px-5">
                          {u.isActive ? (
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                              Active
                            </span>
                          ) : (
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600 border border-slate-200">
                              Deactivated
                            </span>
                          )}
                        </td>
                        <td className="py-3.5 px-5 text-right font-medium">
                          <Link
                            href={`/admin/users/${u.id}`}
                            className="text-[#0b7c74] hover:text-[#085b56] hover:underline"
                          >
                            Edit →
                          </Link>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}
