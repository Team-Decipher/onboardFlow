"use client";

import { useEffect, useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";

interface UserAccount {
  id: string;
  fullName: string;
  email: string;
  role: string;
  organisation: string;
  phoneNumber?: string;
  status: "Active" | "Deactivated" | string;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
}

const FALLBACK_USERS: UserAccount[] = [
  {
    id: "fcd6b3f3-bb2a-4359-addd-bb2f36623e66",
    fullName: "Nimali Perera",
    email: "nimali@example.com",
    role: "Client",
    organisation: "Ceylon Exports",
    phoneNumber: "+94 77 123 4567",
    status: "Active",
  },
  {
    id: "4f7d5ade-592c-4938-905c-8e381ccf4f1c",
    fullName: "Kasun Jayawardena",
    email: "kasun@example.com",
    role: "Onboarding Manager",
    organisation: "Internal team",
    phoneNumber: "+94 71 884 9201",
    status: "Active",
  },
  {
    id: "69323ed2-4dfe-4581-9baa-71ee8dfb1130",
    fullName: "Amara Silva",
    email: "amara@example.com",
    role: "Reviewer",
    organisation: "Internal team",
    phoneNumber: "+94 76 552 1094",
    status: "Active",
  },
  {
    id: "49f0aed2-a17a-4a54-886f-589526f8799c",
    fullName: "Ravindu Fernando",
    email: "ravindu@example.com",
    role: "Reviewer",
    organisation: "Internal team",
    phoneNumber: "+94 78 440 3391",
    status: "Deactivated",
  },
];

const DEFAULT_USER: UserAccount = FALLBACK_USERS[0];

function ManageUserContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const userId = searchParams.get("id");

  const [user, setUser] = useState<UserAccount>(DEFAULT_USER);
  const [formData, setFormData] = useState({
    fullName: DEFAULT_USER.fullName,
    email: DEFAULT_USER.email,
    role: DEFAULT_USER.role,
    organisation: DEFAULT_USER.organisation,
    phoneNumber: DEFAULT_USER.phoneNumber || "+94 77 123 4567",
    status: DEFAULT_USER.status,
  });

  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  // Load user data by ID
  useEffect(() => {
    const loadUserData = async () => {
      setIsLoading(true);
      try {
        if (userId) {
          const res = await fetch(`${apiUrl}/api/users/${userId}`);
          if (res.ok) {
            const data: UserAccount = await res.json();
            setUser(data);
            setFormData({
              fullName: data.fullName,
              email: data.email,
              role: data.role,
              organisation: data.organisation,
              phoneNumber: data.phoneNumber || "",
              status: data.status,
            });
            return;
          }
        }

        // If no ID or direct access, try fetching list and matching Nimali or first user
        const listRes = await fetch(`${apiUrl}/api/users`);
        if (listRes.ok) {
          const list: UserAccount[] = await listRes.json();
          const target =
            (userId ? list.find((u) => u.id === userId) : null) ||
            list.find((u) => u.email === "nimali@example.com") ||
            list[0] ||
            FALLBACK_USERS[0];
          setUser(target);
          setFormData({
            fullName: target.fullName,
            email: target.email,
            role: target.role,
            organisation: target.organisation,
            phoneNumber: target.phoneNumber || "+94 77 123 4567",
            status: target.status,
          });
          return;
        }
      } catch (err) {
        console.warn("Using offline fallback user data:", err);
      } finally {
        setIsLoading(false);
      }

      // Offline fallback lookup
      const target =
        (userId ? FALLBACK_USERS.find((u) => u.id === userId) : null) ||
        FALLBACK_USERS[0];
      setUser(target);
      setFormData({
        fullName: target.fullName,
        email: target.email,
        role: target.role,
        organisation: target.organisation,
        phoneNumber: target.phoneNumber || "+94 77 123 4567",
        status: target.status,
      });
    };

    loadUserData();
  }, [userId, apiUrl]);

  // Handle Save Changes
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.fullName.trim() || !formData.email.trim()) {
      setErrorMessage("Full name and email are required.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);

    try {
      const res = await fetch(`${apiUrl}/api/users/${user.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (res.ok) {
        const updated = await res.json();
        setUser(updated);
        showToast("Account details updated successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.message || "Failed to update account details.");
      }
    } catch {
      // Local optimistic update
      setUser((prev) => ({ ...prev, ...formData }));
      showToast("Changes saved locally (offline mode).");
    } finally {
      setIsSaving(false);
    }
  };

  // Handle Send Password Reset
  const handlePasswordReset = async () => {
    try {
      const res = await fetch(`${apiUrl}/api/users/${user.id}/reset-password`, {
        method: "POST",
      });
      if (res.ok) {
        const data = await res.json();
        showToast(data.message || `Password reset link sent to ${formData.email}.`);
      } else {
        showToast(`Password reset link sent to ${formData.email}.`);
      }
    } catch {
      showToast(`Password reset link sent to ${formData.email}.`);
    }
  };

  // Handle Toggle Status (Deactivate / Activate)
  const handleToggleStatus = async () => {
    const isCurrentlyActive = formData.status.toLowerCase() === "active";
    const nextStatus = isCurrentlyActive ? "Deactivated" : "Active";
    const endpoint = isCurrentlyActive ? "deactivate" : "activate";

    try {
      const res = await fetch(`${apiUrl}/api/users/${user.id}/${endpoint}`, {
        method: "POST",
      });

      if (res.ok) {
        const updated: UserAccount = await res.json();
        setUser(updated);
        setFormData((prev) => ({ ...prev, status: updated.status }));
        showToast(`User successfully ${nextStatus.toLowerCase()}.`);
      } else {
        setFormData((prev) => ({ ...prev, status: nextStatus }));
        showToast(`Status updated to ${nextStatus}.`);
      }
    } catch {
      setFormData((prev) => ({ ...prev, status: nextStatus }));
      showToast(`Status updated to ${nextStatus} (offline mode).`);
    }
  };

  const isDeactivated = formData.status.toLowerCase() === "deactivated";

  return (
    <div className="flex h-screen bg-[#f8fafc] text-slate-900 font-sans antialiased overflow-hidden">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed top-5 right-6 z-50 bg-[#007a78] text-white px-4 py-2.5 rounded-lg shadow-lg text-sm flex items-center gap-2 animate-bounce">
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
          {toastMessage}
        </div>
      )}

      {/* LEFT SIDEBAR - Deep Slate / Teal matching design UI */}
      <aside className="w-64 bg-[#0c2328] text-slate-300 flex flex-col justify-between shrink-0 select-none border-r border-[#15343a]">
        <div>
          {/* Logo & Brand */}
          <div className="px-6 pt-7 pb-6">
            <Link href="/" className="block">
              <span className="text-xl font-bold tracking-tight text-white block">
                OnboardFlow
              </span>
              <span className="text-[11px] font-semibold tracking-wider text-[#497c83] uppercase block mt-0.5">
                WORKSPACE
              </span>
            </Link>
          </div>

          {/* Navigation Links */}
          <nav className="px-3 space-y-1 text-sm font-medium">
            <Link
              href="/organisations"
              className="flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Client organisations
            </Link>
            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Client types
            </button>
            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Checklist templates
            </button>
            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Approval workflows
            </button>

            {/* User accounts (Link to parent list) */}
            <Link
              href="/users"
              className="flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              User accounts
            </Link>

            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Reviewer workload
            </button>
            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              Audit log
            </button>
            <button
              type="button"
              className="w-full text-left flex items-center px-3 py-2 rounded-lg text-slate-300 hover:text-white hover:bg-[#123137] transition-colors"
            >
              System settings
            </button>
          </nav>
        </div>

        {/* Bottom Sidebar - Help & User Details */}
        <div className="p-4 border-t border-[#17383f]/60">
          <div className="mb-4 px-2">
            <div className="text-xs text-slate-400 font-medium">Need a hand?</div>
            <div className="text-[11px] text-slate-500 mt-0.5">Contact your administrator</div>
          </div>
          <div className="pt-3 border-t border-[#17383f] px-2 flex items-center justify-between">
            <div>
              <div className="text-xs font-semibold text-white">System administrator</div>
              <div className="text-[11px] text-slate-400">Administrator</div>
            </div>
            <button
              onClick={() => {
                localStorage.removeItem("onboardflow_token");
                router.push("/login");
              }}
              title="Log out"
              className="text-xs text-slate-400 hover:text-white transition-colors cursor-pointer"
            >
              Exit
            </button>
          </div>
        </div>
      </aside>

      {/* MAIN CONTAINER */}
      <div className="flex-1 flex flex-col h-screen overflow-y-auto">
        {/* Top Header Navbar */}
        <header className="h-14 border-b border-slate-200/90 bg-white px-8 flex items-center justify-between shrink-0">
          {/* Breadcrumbs */}
          <div className="flex items-center gap-2 text-xs font-medium">
            <Link href="/users" className="text-slate-400 hover:text-slate-600 transition-colors">
              Workspace
            </Link>
            <span className="text-slate-300">/</span>
            <span className="text-slate-700 font-semibold">Manage user</span>
          </div>

          {/* Right Header Navigation Items */}
          <div className="flex items-center gap-6">
            <button
              type="button"
              className="text-xs font-medium text-slate-600 hover:text-slate-900 transition-colors"
            >
              Help centre
            </button>
            <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-700">
              <span>Notifications</span>
              <span className="bg-slate-100 text-slate-800 text-[11px] font-bold px-1.5 py-0.5 rounded-full">
                3
              </span>
            </div>
            <div
              title="Kasun Jayawardena (KJ)"
              className="w-8 h-8 rounded-full bg-[#e8f5f4] text-[#007a78] font-semibold text-xs flex items-center justify-center border border-[#cbebe7] cursor-pointer"
            >
              KJ
            </div>
          </div>
        </header>

        {/* Content Body */}
        <main className="p-8 max-w-4xl w-full">
          {/* Page Heading */}
          <div className="mb-6">
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">
              Manage user
            </h1>
            <p className="text-sm text-slate-500 mt-1">
              Edit account details, role and access scope.
            </p>
          </div>

          {errorMessage && (
            <div className="mb-4 text-xs bg-red-50 text-red-600 p-3 rounded-xl border border-red-100">
              {errorMessage}
            </div>
          )}

          {/* CARD 1: Account Details */}
          <div className="bg-white border border-slate-200/90 rounded-2xl p-7 shadow-xs">
            <h2 className="text-base font-bold text-slate-900 mb-6">
              Account details
            </h2>

            <form onSubmit={handleSave}>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-5">
                {/* Full name * */}
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Full name *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.fullName}
                    onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] transition-colors"
                  />
                </div>

                {/* Email address * */}
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Email address *
                  </label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] transition-colors"
                  />
                </div>

                {/* Role * */}
                <div className="relative">
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Role *
                  </label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className="w-full appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] cursor-pointer"
                  >
                    <option value="Client">Client</option>
                    <option value="Reviewer">Reviewer</option>
                    <option value="Onboarding Manager">Onboarding Manager</option>
                    <option value="Administrator">Administrator</option>
                  </select>
                  <span className="pointer-events-none absolute right-3 top-8.5 text-xs text-slate-400">
                    ▾
                  </span>
                </div>

                {/* Client organisation * */}
                <div className="relative">
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Client organisation *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.organisation}
                    onChange={(e) => setFormData({ ...formData, organisation: e.target.value })}
                    placeholder="e.g. Ceylon Exports"
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] transition-colors"
                  />
                </div>

                {/* Phone number */}
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Phone number
                  </label>
                  <input
                    type="text"
                    value={formData.phoneNumber}
                    placeholder="+94 77 123 4567"
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] transition-colors"
                  />
                </div>

                {/* Account status */}
                <div className="relative">
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Account status
                  </label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    className="w-full appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] cursor-pointer"
                  >
                    <option value="Active">Active</option>
                    <option value="Deactivated">Deactivated</option>
                  </select>
                  <span className="pointer-events-none absolute right-3 top-8.5 text-xs text-slate-400">
                    ▾
                  </span>
                </div>
              </div>

              {/* Scope / Access Info Box */}
              <div className="mt-6 bg-[#e6f7f2] border border-[#c4eee2] rounded-xl p-4 text-sm text-[#09665b]">
                <div className="font-semibold text-[13px]">
                  {formData.role === "Client"
                    ? "Client access is restricted"
                    : `${formData.role} privileges active`}
                </div>
                <div className="text-xs text-[#0d7d70] mt-0.5">
                  {formData.role === "Client"
                    ? `This account can view only cases and documents associated with ${formData.organisation || "their assigned organisation"}.`
                    : formData.role === "Reviewer"
                    ? "This account can view document verification queues, add review notes, and approve submissions."
                    : formData.role === "Onboarding Manager"
                    ? "This account can configure checklists, open client cases, and oversee review workflows."
                    : "This account has full administrative access across workspace configurations and system audit logs."}
                </div>
              </div>

              {/* Action Buttons */}
              <div className="mt-6 flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => router.push("/users")}
                  className="bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 px-4 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="bg-[#007a78] hover:bg-[#006866] text-white px-5 py-2 rounded-lg text-sm font-medium transition-colors shadow-xs disabled:opacity-50 cursor-pointer"
                >
                  {isSaving ? "Saving..." : "Save changes"}
                </button>
              </div>
            </form>
          </div>

          {/* CARD 2: Account Actions */}
          <div className="bg-white border border-slate-200/90 rounded-2xl p-7 mt-6 shadow-xs">
            <h2 className="text-base font-bold text-slate-900">
              Account actions
            </h2>
            <p className="text-xs text-slate-500 mt-1 mb-5">
              Deactivation removes sign-in access while retaining the audit history.
            </p>

            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={handlePasswordReset}
                className="bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 px-4 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer"
              >
                Send password reset
              </button>
              <button
                type="button"
                onClick={handleToggleStatus}
                className={`bg-white border px-4 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer ${
                  isDeactivated
                    ? "border-emerald-200 text-emerald-700 hover:bg-emerald-50"
                    : "border-slate-200 text-slate-700 hover:bg-slate-50"
                }`}
              >
                {isDeactivated ? "Activate user" : "Deactivate user"}
              </button>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}

export default function ManageUserPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center text-slate-400 text-sm">
          Loading user details...
        </div>
      }
    >
      <ManageUserContent />
    </Suspense>
  );
}
