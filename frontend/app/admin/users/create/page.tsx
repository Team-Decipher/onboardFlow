"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { ClientOrganisation } from "@/features/auth/types/auth.types";

export default function CreateUserPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("Reviewer");
  const [clientOrganisationId, setClientOrganisationId] = useState("");
  const [organisations, setOrganisations] = useState<ClientOrganisation[]>([]);
  const [password, setPassword] = useState("password123");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    async function loadOrgs() {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const res = await fetch(`${apiUrl}/api/users/organisations`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (res.ok) {
          const data = await res.json();
          setOrganisations(data);
          if (data.length > 0) {
            setClientOrganisationId(data[0].id);
          }
        }
      } catch {
        // Ignore fallback
      }
    }
    loadOrgs();
  }, [apiUrl]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setErrorMessage(null);

    if (role === "Client" && !clientOrganisationId) {
      setErrorMessage("Client organisation is required when role is Client.");
      setIsLoading(false);
      return;
    }

    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/users`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          fullName,
          email,
          role,
          clientOrganisationId: role === "Client" ? clientOrganisationId : null,
          password,
        }),
      });

      if (res.ok) {
        router.push("/admin/users");
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.message || "Failed to create user.");
      }
    } catch {
      setErrorMessage("Network error: Could not reach the backend API.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <RoleGuard allowedRoles={["Administrator"]}>
      <WorkspaceLayout>
        <div className="max-w-2xl mx-auto space-y-6">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">
              Create user
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Add an internal team member or a client account.
            </p>
          </div>

          <div className="bg-white rounded-2xl p-7 border border-slate-200/80 shadow-xs">
            {errorMessage && (
              <div className="mb-5 p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs">
                {errorMessage}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Full name *
                </label>
                <input
                  type="text"
                  required
                  placeholder="Enter full name"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Email address *
                </label>
                <input
                  type="email"
                  required
                  placeholder="name@company.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
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
                    {organisations.map((org) => (
                      <option key={org.id} value={org.id}>
                        {org.name} ({org.category})
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Informational callout from design */}
              <div className="p-3.5 rounded-xl bg-teal-50 border border-teal-100 text-teal-800 text-[11px] leading-relaxed">
                <span className="font-semibold">Choose an access role:</span>{" "}
                Administrator · Onboarding Manager · Reviewer · Client. Client accounts must be linked to an organisation.
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Initial Password
                </label>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-xs text-slate-800 bg-slate-50/50 focus:outline-none focus:ring-2 focus:ring-[#0b7c74]"
                />
              </div>

              <div className="flex items-center gap-3 pt-3">
                <button
                  type="submit"
                  disabled={isLoading}
                  className="py-2.5 px-5 rounded-xl bg-[#0b7c74] hover:bg-[#096963] text-white font-semibold text-xs shadow-sm transition-colors disabled:opacity-50"
                >
                  {isLoading ? "Creating..." : "Create user"}
                </button>
                <Link
                  href="/admin/users"
                  className="py-2.5 px-4 rounded-xl border border-slate-200 text-xs font-medium text-slate-600 hover:bg-slate-50 transition-colors"
                >
                  Cancel
                </Link>
              </div>
            </form>
          </div>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}

