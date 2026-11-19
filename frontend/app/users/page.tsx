"use client";

import { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";

export interface UserAccount {
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

const INITIAL_FALLBACK_USERS: UserAccount[] = [
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
    id: "fcd6b3f3-bb2a-4359-addd-bb2f36623e66",
    fullName: "Nimali Perera",
    email: "nimali@example.com",
    role: "Client",
    organisation: "Ceylon Exports",
    phoneNumber: "+94 77 123 4567",
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

export default function UserAccountsPage() {
  const router = useRouter();
  const [users, setUsers] = useState<UserAccount[]>(INITIAL_FALLBACK_USERS);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedRole, setSelectedRole] = useState("All");
  const [selectedStatus, setSelectedStatus] = useState("All");

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserAccount | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  // Form states for Add / Edit
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    role: "Reviewer",
    organisation: "Internal team",
    status: "Active",
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  // Fetch users from ASP.NET Core API
  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      const res = await fetch(`${apiUrl}/api/users`);
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) {
          setUsers(data);
        } else {
          setUsers(INITIAL_FALLBACK_USERS);
        }
      } else {
        setUsers(INITIAL_FALLBACK_USERS);
      }
    } catch (err) {
      console.warn("Using offline fallback users:", err);
      setUsers(INITIAL_FALLBACK_USERS);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, []);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  // Filtered users
  const filteredUsers = useMemo(() => {
    return users.filter((u) => {
      const matchesSearch =
        u.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.email.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesRole =
        selectedRole === "All" ||
        u.role.toLowerCase() === selectedRole.toLowerCase();

      const matchesStatus =
        selectedStatus === "All" ||
        u.status.toLowerCase() === selectedStatus.toLowerCase();

      return matchesSearch && matchesRole && matchesStatus;
    });
  }, [users, searchTerm, selectedRole, selectedStatus]);

  // Open Add Modal
  const openAddModal = () => {
    setFormData({
      fullName: "",
      email: "",
      role: "Reviewer",
      organisation: "Internal team",
      status: "Active",
    });
    setFormError(null);
    setIsAddModalOpen(true);
  };

  // Open Edit Modal
  const openEditModal = (user: UserAccount) => {
    setEditingUser(user);
    setFormData({
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      organisation: user.organisation,
      status: user.status,
    });
    setFormError(null);
    setDeleteConfirmId(null);
  };

  // Handle Add User Submit
  const handleAddSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.fullName.trim() || !formData.email.trim()) {
      setFormError("Full name and email are required.");
      return;
    }

    setIsSubmitting(true);
    setFormError(null);

    try {
      const res = await fetch(`${apiUrl}/api/users`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (res.ok) {
        const newUser = await res.json();
        setUsers((prev) => [newUser, ...prev]);
        setIsAddModalOpen(false);
        showToast("User account created successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setFormError(err?.message || "Failed to create user. Please check details.");
      }
    } catch {
      // Local optimistic fallback
      const localNewUser: UserAccount = {
        id: `usr-${Date.now()}`,
        ...formData,
      };
      setUsers((prev) => [localNewUser, ...prev]);
      setIsAddModalOpen(false);
      showToast("User added (local offline mode).");
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle Edit User Submit
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingUser) return;
    if (!formData.fullName.trim() || !formData.email.trim()) {
      setFormError("Full name and email are required.");
      return;
    }

    setIsSubmitting(true);
    setFormError(null);

    try {
      const res = await fetch(`${apiUrl}/api/users/${editingUser.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (res.ok) {
        const updated = await res.json();
        setUsers((prev) =>
          prev.map((u) => (u.id === editingUser.id ? updated : u))
        );
        setEditingUser(null);
        showToast("User account updated successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setFormError(err?.message || "Failed to update user.");
      }
    } catch {
      // Local optimistic fallback
      setUsers((prev) =>
        prev.map((u) => (u.id === editingUser.id ? { ...u, ...formData } : u))
      );
      setEditingUser(null);
      showToast("User updated (local offline mode).");
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle Delete User
  const handleDeleteUser = async (id: string) => {
    setIsSubmitting(true);
    try {
      const res = await fetch(`${apiUrl}/api/users/${id}`, {
        method: "DELETE",
      });
      if (res.ok || res.status === 204) {
        setUsers((prev) => prev.filter((u) => u.id !== id));
        setEditingUser(null);
        showToast("User account removed.");
      } else {
        const err = await res.json().catch(() => null);
        setFormError(err?.message || "Failed to delete user.");
      }
    } catch {
      setUsers((prev) => prev.filter((u) => u.id !== id));
      setEditingUser(null);
      showToast("User removed (local offline mode).");
    } finally {
      setIsSubmitting(false);
      setDeleteConfirmId(null);
    }
  };

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

      {/* LEFT SIDEBAR - Deep Slate / Teal matching reference UI */}
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
              href="/dashboard"
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

            {/* ACTIVE: User accounts */}
            <div className="flex items-center px-3.5 py-2.5 rounded-lg bg-[#007a78] text-white font-medium shadow-sm">
              User accounts
            </div>

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
              className="text-xs text-slate-400 hover:text-white transition-colors"
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
          {/* Breadcrumb */}
          <div className="flex items-center gap-2 text-xs font-medium text-slate-400">
            <span>Workspace</span>
            <span className="text-slate-300">/</span>
            <span className="text-slate-700 font-semibold">User accounts</span>
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
        <main className="p-8 max-w-6xl w-full">
          {/* Page Title & Add User Button */}
          <div className="flex items-start justify-between mb-6">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                User accounts
              </h1>
              <p className="text-sm text-slate-500 mt-1">
                Give each person the access they need.
              </p>
            </div>
            <button
              onClick={openAddModal}
              className="bg-[#007a78] hover:bg-[#006866] text-white px-4 py-2 rounded-lg text-sm font-medium flex items-center shadow-sm transition-all duration-150 cursor-pointer"
            >
              <span>+ Add user</span>
            </button>
          </div>

          {/* Search & Filter Toolbar */}
          <div className="flex flex-wrap items-center gap-3 mb-6">
            {/* Search Input */}
            <div className="relative w-80">
              <input
                type="text"
                placeholder="Search name or email"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 shadow-sm focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] transition-all"
              />
              {searchTerm && (
                <button
                  onClick={() => setSearchTerm("")}
                  className="absolute right-2.5 top-2.5 text-xs text-slate-400 hover:text-slate-600"
                >
                  ✕
                </button>
              )}
            </div>

            {/* Role Filter Dropdown */}
            <div className="relative">
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-700 shadow-sm focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] cursor-pointer"
              >
                <option value="All">Role: All</option>
                <option value="Onboarding Manager">Role: Onboarding Manager</option>
                <option value="Reviewer">Role: Reviewer</option>
                <option value="Client">Role: Client</option>
                <option value="Administrator">Role: Administrator</option>
              </select>
              <span className="pointer-events-none absolute right-2.5 top-2.5 text-xs text-slate-400">
                ▾
              </span>
            </div>

            {/* Status Filter Dropdown */}
            <div className="relative">
              <select
                value={selectedStatus}
                onChange={(e) => setSelectedStatus(e.target.value)}
                className="appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-700 shadow-sm focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78] cursor-pointer"
              >
                <option value="All">Status: All</option>
                <option value="Active">Status: Active</option>
                <option value="Deactivated">Status: Deactivated</option>
              </select>
              <span className="pointer-events-none absolute right-2.5 top-2.5 text-xs text-slate-400">
                ▾
              </span>
            </div>

            {/* Active filter counter */}
            {(searchTerm || selectedRole !== "All" || selectedStatus !== "All") && (
              <button
                onClick={() => {
                  setSearchTerm("");
                  setSelectedRole("All");
                  setSelectedStatus("All");
                }}
                className="text-xs text-[#007a78] hover:underline font-medium ml-1"
              >
                Reset filters
              </button>
            )}
          </div>

          {/* User Accounts Table Card */}
          <div className="bg-white border border-slate-200/90 rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-100 bg-white">
                  <th className="py-3.5 px-6 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    USER
                  </th>
                  <th className="py-3.5 px-6 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    ROLE
                  </th>
                  <th className="py-3.5 px-6 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    ORGANISATION
                  </th>
                  <th className="py-3.5 px-6 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    STATUS
                  </th>
                  <th className="py-3.5 px-6 text-[11px] font-semibold text-slate-400 uppercase tracking-wider text-right">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {isLoading ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-slate-400 text-sm">
                      <div className="inline-block animate-spin rounded-full h-5 w-5 border-2 border-slate-300 border-t-[#007a78] mr-2 align-middle"></div>
                      Loading accounts...
                    </td>
                  </tr>
                ) : filteredUsers.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-slate-400 text-sm">
                      No user accounts found matching your filters.
                    </td>
                  </tr>
                ) : (
                  filteredUsers.map((user) => {
                    const isActive = user.status.toLowerCase() === "active";
                    return (
                      <tr
                        key={user.id}
                        className="hover:bg-slate-50/70 transition-colors group"
                      >
                        {/* USER Name & Email */}
                        <td className="py-4 px-6 align-middle">
                          <div className="font-semibold text-sm text-slate-900">
                            {user.fullName}
                          </div>
                          <div className="text-xs text-slate-400 mt-0.5">
                            {user.email}
                          </div>
                        </td>

                        {/* ROLE */}
                        <td className="py-4 px-6 align-middle text-sm text-slate-700">
                          {user.role}
                        </td>

                        {/* ORGANISATION */}
                        <td className="py-4 px-6 align-middle text-sm text-slate-700">
                          {user.organisation}
                        </td>

                        {/* STATUS */}
                        <td className="py-4 px-6 align-middle text-sm">
                          <span
                            className={
                              isActive
                                ? "text-slate-700 font-normal"
                                : "text-slate-400 font-normal"
                            }
                          >
                            {user.status}
                          </span>
                        </td>

                        {/* ACTION: Edit -> */}
                        <td className="py-4 px-6 align-middle text-right">
                          <Link
                            href={`/manage-user?id=${user.id}`}
                            className="text-[#007a78] hover:text-[#005a58] font-medium text-sm inline-flex items-center gap-1 transition-colors cursor-pointer"
                          >
                            <span>Edit</span>
                            <span>→</span>
                          </Link>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </main>
      </div>

      {/* ADD USER MODAL */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl max-w-md w-full border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-slate-900">+ Add user</h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Create a new team member or client account.
                </p>
              </div>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg leading-none p-1"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleAddSubmit} className="p-6 space-y-4">
              {formError && (
                <div className="text-xs bg-red-50 text-red-600 p-2.5 rounded-lg border border-red-100">
                  {formError}
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Full name *
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Kasun Jayawardena"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Email address *
                </label>
                <input
                  type="email"
                  required
                  placeholder="e.g. kasun@example.com"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Role
                  </label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                  >
                    <option value="Onboarding Manager">Onboarding Manager</option>
                    <option value="Reviewer">Reviewer</option>
                    <option value="Client">Client</option>
                    <option value="Administrator">Administrator</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Status
                  </label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                  >
                    <option value="Active">Active</option>
                    <option value="Deactivated">Deactivated</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Organisation
                </label>
                <input
                  type="text"
                  placeholder="e.g. Internal team or Ceylon Exports"
                  value={formData.organisation}
                  onChange={(e) => setFormData({ ...formData, organisation: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2.5">
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="bg-[#007a78] hover:bg-[#006866] text-white px-4 py-2 rounded-lg text-xs font-semibold transition-colors disabled:opacity-50"
                >
                  {isSubmitting ? "Adding..." : "Add user"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* EDIT USER MODAL */}
      {editingUser && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl max-w-md w-full border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-slate-900">Edit user account</h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Update role, details or access status.
                </p>
              </div>
              <button
                onClick={() => setEditingUser(null)}
                className="text-slate-400 hover:text-slate-600 text-lg leading-none p-1"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleEditSubmit} className="p-6 space-y-4">
              {formError && (
                <div className="text-xs bg-red-50 text-red-600 p-2.5 rounded-lg border border-red-100">
                  {formError}
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Full name *
                </label>
                <input
                  type="text"
                  required
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Email address *
                </label>
                <input
                  type="email"
                  required
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Role
                  </label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                  >
                    <option value="Onboarding Manager">Onboarding Manager</option>
                    <option value="Reviewer">Reviewer</option>
                    <option value="Client">Client</option>
                    <option value="Administrator">Administrator</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Status
                  </label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                  >
                    <option value="Active">Active</option>
                    <option value="Deactivated">Deactivated</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Organisation
                </label>
                <input
                  type="text"
                  value={formData.organisation}
                  onChange={(e) => setFormData({ ...formData, organisation: e.target.value })}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78] focus:border-[#007a78]"
                />
              </div>

              {/* Action buttons */}
              <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
                <div>
                  {deleteConfirmId === editingUser.id ? (
                    <div className="flex items-center gap-2">
                      <span className="text-[11px] text-red-600 font-semibold">Confirm?</span>
                      <button
                        type="button"
                        onClick={() => handleDeleteUser(editingUser.id)}
                        className="text-xs bg-red-600 text-white px-2.5 py-1 rounded hover:bg-red-700 font-medium transition-colors"
                      >
                        Yes, delete
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteConfirmId(null)}
                        className="text-xs text-slate-400 hover:text-slate-600"
                      >
                        Cancel
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setDeleteConfirmId(editingUser.id)}
                      className="text-xs text-red-500 hover:text-red-700 font-medium"
                    >
                      Delete user
                    </button>
                  )}
                </div>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setEditingUser(null)}
                    className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 rounded-lg transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="bg-[#007a78] hover:bg-[#006866] text-white px-4 py-2 rounded-lg text-xs font-semibold transition-colors disabled:opacity-50"
                  >
                    {isSubmitting ? "Saving..." : "Save changes"}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
