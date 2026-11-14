"use client";

import { useEffect, useState, Suspense } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { ClientOrganisation } from "@/features/auth/types/auth.types";

function ManageUserContent() {
  const router = useRouter();
  const params = useParams();
  const userId = params?.id as string;

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Reviewer");
  const [clientOrganisationId, setClientOrganisationId] = useState<string>("");
  const [organisations, setOrganisations] = useState<ClientOrganisation[]>([]);
  const [isActive, setIsActive] = useState(true);

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    if (!userId) return;

    let ignore = false;

    async function loadData() {
      try {
        const token = localStorage.getItem("onboardflow_token");

        // 1. Fetch Organisations
        const orgRes = await fetch(`${apiUrl}/api/users/organisations`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (orgRes.ok && !ignore) {
          const orgData = await orgRes.json();
          setOrganisations(orgData);
        }

        // 2. Fetch User Details
        const userRes = await fetch(`${apiUrl}/api/users/${userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!ignore) {
          if (userRes.ok) {
            const userData = await userRes.json();
            setFullName(userData.fullName);
            setEmail(userData.email);
            setRole(userData.role);
            setClientOrganisationId(userData.clientOrganisationId || "");
            setIsActive(userData.isActive);
          } else {
            setErrorMessage("Failed to load user information.");
          }
          setIsLoading(false);
        }
      } catch {
        if (!ignore) {
          setErrorMessage("Network error: Could not connect to API.");
          setIsLoading(false);
        }
      }
    }

    loadData();

    return () => {
      ignore = true;
    };
  }, [apiUrl, userId]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    if (role === "Client" && !clientOrganisationId) {
      setErrorMessage("Client organisation is required when role is Client.");
      setIsSaving(false);
      return;
    }

    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/users/${userId}`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          fullName,
          role,
          clientOrganisationId: role === "Client" ? clientOrganisationId : null,
          isActive,
        }),
      });

      if (res.ok) {
        setSuccessMessage("User details saved successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.message || "Failed to update user.");
      }
    } catch {
      setErrorMessage("Network error: Could not reach the backend API.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleActive = async () => {
    setIsSaving(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    const nextState = !isActive;
    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/users/${userId}`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          isActive: nextState,
        }),
      });

      if (res.ok) {
        setIsActive(nextState);
        setSuccessMessage(
          nextState
            ? "User account has been activated."
            : "User account has been deactivated."
        );
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.message || "Failed to update user status.");
      }
    } catch {
      setErrorMessage("Network error: Could not reach backend API.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <RoleGuard allowedRoles={["Administrator"]}>
      <WorkspaceLayout>
        <div className="max-w-2xl mx-auto space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                Manage user
              </h1>
              <p className="text-xs text-slate-500 mt-1">
                Edit account details, role and access scope.
              </p>
            </div>

            <Link
              href="/admin/users"
              className="text-xs text-slate-500 hover:text-slate-800 transition-colors"
            >
              ← Back to user accounts
            </Link>
          </div>

          {isLoading ? (
            <div className="bg-white rounded-2xl p-8 border border-slate-200 text-center text-xs text-slate-400">
              Loading user details...
            </div>
          ) : (
            <div className="bg-white rounded-2xl p-7 border border-slate-200/80 shadow-xs space-y-6">
              {errorMessage && (
                <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs">
                  {errorMessage}
                </div>
              )}

              {successMessage && (
                <div className="p-3.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs">
                  {successMessage}
                </div>
              )}

              <form onSubmit={handleSave} className="space-y-5">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Full name *
                  </label>
                  <input
                    type="text"
                    required
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Email address
                  </label>
                  <input
                    type="email"
                    disabled
                    value={email}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-500 bg-slate-100 cursor-not-allowed"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Role *
                    </label>
                    <select
                      value={role}
                      onChange={(e) => setRole(e.target.value)}
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
                    >
                      <option value="Administrator">Administrator</option>
                      <option value="OnboardingManager">Onboarding Manager</option>
                      <option value="Reviewer">Reviewer</option>
                      <option value="Client">Client</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Client organisation {role === "Client" && "*"}
                    </label>
                    <select
                      disabled={role !== "Client"}
                      value={clientOrganisationId}
                      onChange={(e) => setClientOrganisationId(e.target.value)}
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74] disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <option value="">Select organisation...</option>
                      {organisations.map((org) => (
                        <option key={org.id} value={org.id}>
                          {org.name} ({org.category})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {role === "Client" && (
                  <div className="p-3.5 rounded-xl bg-teal-50 border border-teal-100 text-teal-800 text-[11px] leading-relaxed">
                    <span className="font-semibold">Client access is restricted:</span> This account can view only cases and documents associated with their organization.
                  </div>
                )}

                <div className="flex items-center gap-3 pt-2">
                  <button
                    type="submit"
                    disabled={isSaving}
                    className="py-2.5 px-5 rounded-xl bg-[#0b7c74] hover:bg-[#096963] text-white font-semibold text-xs shadow-sm transition-colors disabled:opacity-50"
                  >
                    {isSaving ? "Saving..." : "Save changes"}
                  </button>
                  <button
                    type="button"
                    onClick={() => router.push("/admin/users")}
                    className="py-2.5 px-4 rounded-xl border border-slate-200 text-xs font-medium text-slate-600 hover:bg-slate-50 transition-colors"
                  >
                    Cancel
                  </button>
                </div>
              </form>

              {/* Account Actions Section */}
              <div className="pt-6 border-t border-slate-100">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">
                  Account actions
                </h3>
                <div className="flex flex-wrap items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setSuccessMessage("Password reset instructions sent.")}
                    className="py-2 px-3.5 rounded-xl border border-slate-200 text-xs font-medium text-slate-700 hover:bg-slate-50 transition-colors"
                  >
                    Send password reset
                  </button>

                  <button
                    type="button"
                    onClick={handleToggleActive}
                    disabled={isSaving}
                    className={`py-2 px-3.5 rounded-xl text-xs font-semibold transition-colors ${
                      isActive
                        ? "bg-red-50 text-red-700 border border-red-200 hover:bg-red-100"
                        : "bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100"
                    }`}
                  >
                    {isActive ? "Deactivate user" : "Activate user"}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}

export default function ManageUserPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f3f6f7] flex items-center justify-center text-xs text-slate-400">
          Loading user editor...
        </div>
      }
    >
      <ManageUserContent />
    </Suspense>
  );
}
