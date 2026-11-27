"use client";

import { useCallback, useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { withOrganisationOverrides } from "@/features/organisations/data";
import type { ClientOrganisation } from "@/features/organisations/types";

export const INITIAL_ORGANISATIONS: ClientOrganisation[] = [
  {
    id: "f1a23456-1111-2222-3333-000000000001",
    clientCode: "CLI-0018",
    name: "Ceylon Exports (Pvt) Ltd",
    city: "Colombo",
    clientType: "Enterprise",
    primaryContact: "Nimali Perera",
    contactEmail: "nimali@example.com",
    phoneNumber: "+94 11 234 5678",
    businessRegistrationNumber: "PV 123456",
    businessAddress: "42 Marine Drive, Colombo 03",
    activeCasesCount: 1,
    casesSummary: "1 active",
    status: "Active",
    linkedCases: [
      {
        id: "case-018",
        organisationId: "f1a23456-1111-2222-3333-000000000001",
        caseNumber: "OBF-2026-018",
        title: "Enterprise Statutory KYC & Compliance Onboarding",
        status: "Active",
        progressPercentage: 67,
        reviewerName: "Kasun Jayawardena",
        targetCompletionDate: "2026-12-05",
      },
    ],
  },
  {
    id: "f1a23456-1111-2222-3333-000000000002",
    clientCode: "CLI-0017",
    name: "Lanka Foods",
    city: "Kandy",
    clientType: "SME",
    primaryContact: "Dilan Fernando",
    contactEmail: "dilan@lankafoods.lk",
    phoneNumber: "+94 81 223 4567",
    businessRegistrationNumber: "PV 987654",
    businessAddress: "15 Peradeniya Road, Kandy",
    activeCasesCount: 2,
    casesSummary: "2 active",
    status: "Active",
    linkedCases: [
      {
        id: "case-017a",
        organisationId: "f1a23456-1111-2222-3333-000000000002",
        caseNumber: "OBF-2026-017A",
        title: "SME Fast-Track Supplier Documentation",
        status: "Active",
        progressPercentage: 45,
        reviewerName: "Amara Silva",
        targetCompletionDate: "2026-11-28",
      },
      {
        id: "case-017b",
        organisationId: "f1a23456-1111-2222-3333-000000000002",
        caseNumber: "OBF-2026-017B",
        title: "Export Quality & Standard Certification Verification",
        status: "Active",
        progressPercentage: 80,
        reviewerName: "Kasun Jayawardena",
        targetCompletionDate: "2026-12-10",
      },
    ],
  },
  {
    id: "f1a23456-1111-2222-3333-000000000003",
    clientCode: "CLI-0016",
    name: "Oceanic Partners",
    city: "Galle",
    clientType: "Partner",
    primaryContact: "Shanika de Silva",
    contactEmail: "shanika@oceanicpartners.lk",
    phoneNumber: "+94 91 334 5678",
    businessRegistrationNumber: "PV 456123",
    businessAddress: "88 Fort Light St, Galle",
    activeCasesCount: 1,
    casesSummary: "1 active",
    status: "Active",
    linkedCases: [
      {
        id: "case-016",
        organisationId: "f1a23456-1111-2222-3333-000000000003",
        caseNumber: "OBF-2026-016",
        title: "Channel Partner Legal Agreement & NDA Vetting",
        status: "Active",
        progressPercentage: 50,
        reviewerName: "Amara Silva",
        targetCompletionDate: "2026-12-01",
      },
    ],
  },
  {
    id: "f1a23456-1111-2222-3333-000000000004",
    clientCode: "CLI-0015",
    name: "Lotus Digital",
    city: "Colombo",
    clientType: "SME",
    primaryContact: "Ruwan Jayasuriya",
    contactEmail: "ruwan@lotusdigital.lk",
    phoneNumber: "+94 11 889 0123",
    businessRegistrationNumber: "PV 321654",
    businessAddress: "120 Galle Road, Colombo 03",
    activeCasesCount: 0,
    casesSummary: "Completed",
    status: "Active",
    linkedCases: [
      {
        id: "case-015",
        organisationId: "f1a23456-1111-2222-3333-000000000004",
        caseNumber: "OBF-2026-015",
        title: "Digital Service Provider Compliance Review",
        status: "Completed",
        progressPercentage: 100,
        reviewerName: "Ravindu Fernando",
        targetCompletionDate: "2026-10-15",
      },
    ],
  },
];

const ORGANISATION_NAV_ITEMS = [
  { label: "Overview", href: "/dashboard" },
  { label: "Client organisations", href: "/client-organisations" },
  { label: "Client types", href: "/admin/client-types" },
  { label: "Checklist templates", href: "/admin/checklists" },
  { label: "Approval workflows", href: "/admin/workflows" },
  { label: "User accounts", href: "/admin/users" },
  { label: "Reviewer workload", href: "/admin/reviewer-workload" },
  { label: "Audit log", href: "/admin/audit-log" },
  { label: "System settings", href: "/admin/settings" },
];

function getNextClientCode(organisations: ClientOrganisation[]): string {
  const highestNumber = organisations.reduce((highest, organisation) => {
    const match = /^CLI-(\d+)$/i.exec(organisation.clientCode.trim());
    return match ? Math.max(highest, Number(match[1])) : highest;
  }, 18);

  return `CLI-${String(highestNumber + 1).padStart(4, "0")}`;
}

export default function OrganisationsPage() {
  const router = useRouter();
  const pathname = usePathname();
  const [organisations, setOrganisations] = useState<ClientOrganisation[]>(INITIAL_ORGANISATIONS);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedType, setSelectedType] = useState("All");
  const [selectedStatus, setSelectedStatus] = useState("Active");

  // Add organisation modal state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    clientCode: "",
    city: "Colombo",
    clientType: "Enterprise",
    primaryContact: "",
    contactEmail: "",
    phoneNumber: "",
    businessRegistrationNumber: "",
    businessAddress: "",
    casesSummary: "1 active",
    status: "Active",
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  // Fetch organisations from backend
  const fetchOrganisations = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await fetch(`${apiUrl}/api/organisations`);
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) {
          setOrganisations(data);
        } else {
          setOrganisations(withOrganisationOverrides(INITIAL_ORGANISATIONS));
        }
      } else {
        setOrganisations(withOrganisationOverrides(INITIAL_ORGANISATIONS));
      }
    } catch {
      setOrganisations(withOrganisationOverrides(INITIAL_ORGANISATIONS));
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void fetchOrganisations();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [fetchOrganisations]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  // Filtered organisations
  const filteredOrganisations = useMemo(() => {
    return organisations.filter((org) => {
      const matchesSearch =
        searchTerm === "" ||
        org.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        org.clientCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
        org.city.toLowerCase().includes(searchTerm.toLowerCase()) ||
        org.primaryContact.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (org.businessRegistrationNumber &&
          org.businessRegistrationNumber.toLowerCase().includes(searchTerm.toLowerCase()));

      const matchesType =
        selectedType === "All" ||
        org.clientType.toLowerCase() === selectedType.toLowerCase();

      const matchesStatus =
        selectedStatus === "All" ||
        org.status.toLowerCase() === selectedStatus.toLowerCase();

      return matchesSearch && matchesType && matchesStatus;
    });
  }, [organisations, searchTerm, selectedType, selectedStatus]);

  // Open Add Modal
  const openAddModal = () => {
    setFormData({
      name: "",
      clientCode: getNextClientCode(organisations),
      city: "Colombo",
      clientType: "Enterprise",
      primaryContact: "",
      contactEmail: "",
      phoneNumber: "+94 11 ",
      businessRegistrationNumber: "",
      businessAddress: "",
      casesSummary: "1 active",
      status: "Active",
    });
    setFormError(null);
    setIsAddModalOpen(true);
  };

  // Submit Add Organisation
  const handleAddSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.primaryContact.trim()) {
      setFormError("Organisation name and primary contact are required.");
      return;
    }

    setIsSubmitting(true);
    setFormError(null);

    try {
      const res = await fetch(`${apiUrl}/api/organisations`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (res.ok) {
        const newOrg = await res.json();
        setOrganisations((prev) => [newOrg, ...prev]);
        setIsAddModalOpen(false);
        showToast("Organisation added successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setFormError(err?.message || "Failed to create organisation.");
      }
    } catch {
      // Local offline fallback
      const localNewOrg: ClientOrganisation = {
        id: `org-${Date.now()}`,
        ...formData,
        activeCasesCount: 1,
        linkedCases: [
          {
            id: `case-${Date.now()}`,
            organisationId: `org-${Date.now()}`,
            caseNumber: `OBF-2026-${Math.floor(100 + Math.random() * 900)}`,
            title: `${formData.name} Onboarding Workflow`,
            status: "Active",
            progressPercentage: 15,
            reviewerName: "Kasun Jayawardena",
            targetCompletionDate: "2026-12-15",
          },
        ],
      };
      setOrganisations((prev) => [localNewOrg, ...prev]);
      setIsAddModalOpen(false);
      showToast("Organisation created (local offline mode).");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex h-screen bg-[#f8fafc] text-slate-800 font-sans antialiased overflow-hidden">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed top-5 right-5 z-50 flex items-center gap-2 bg-[#007a78] text-white px-4 py-3 rounded-lg shadow-lg text-sm animate-in fade-in slide-in-from-top-2 duration-200">
          <svg className="w-5 h-5 text-emerald-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
          </svg>
          <span>{toastMessage}</span>
        </div>
      )}

      {/* SIDEBAR NAVIGATION */}
      <aside className="w-64 bg-[#0d282f] text-white flex flex-col justify-between shrink-0 shadow-lg select-none">
        <div>
          {/* Logo Header */}
          <div className="p-6 pb-5">
            <Link href="/dashboard" className="block">
              <h1 className="text-xl font-bold tracking-tight text-white flex items-center gap-2">
                OnboardFlow
              </h1>
              <span className="text-[10px] tracking-widest uppercase font-semibold text-teal-400 block mt-0.5">
                WORKSPACE
              </span>
            </Link>
          </div>

          {/* Navigation Links */}
          <nav className="px-3 space-y-1 text-sm font-medium">
            {ORGANISATION_NAV_ITEMS.map((item) => {
              const isActive =
                pathname === item.href ||
                (item.href === "/client-organisations" && pathname === "/organisations") ||
                (item.href === "/admin/checklists" &&
                  pathname.startsWith("/admin/checklist-templates"));

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={isActive ? "page" : undefined}
                  className={`flex items-center px-3.5 py-2.5 rounded-lg transition-colors ${
                    isActive
                      ? "bg-[#007a78] text-white font-medium shadow-sm"
                      : "text-slate-300 hover:text-white hover:bg-[#123137]"
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* Bottom Sidebar - Help & User Info */}
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
            <span className="text-slate-700 font-semibold">Client organisations</span>
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
          {/* Page Title & Add Organisation Button */}
          <div className="flex items-start justify-between mb-6">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                Client organisations
              </h1>
              <p className="text-sm text-slate-500 mt-1">
                Maintain client records and their onboarding contacts.
              </p>
            </div>
            <button
              onClick={openAddModal}
              className="bg-[#007a78] hover:bg-[#006866] text-white px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-1.5 shadow-sm transition-all duration-150 cursor-pointer"
            >
              <span>+ Add organisation</span>
            </button>
          </div>

          {/* Search & Filter Toolbar */}
          <div className="flex flex-wrap items-center gap-3 mb-6">
            {/* Search Input */}
            <div className="relative w-72">
              <input
                type="text"
                placeholder="Search organisations"
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

            {/* Client Type Filter */}
            <div className="relative">
              <select
                value={selectedType}
                onChange={(e) => setSelectedType(e.target.value)}
                className="appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-700 shadow-sm focus:outline-none focus:ring-1 focus:ring-[#007a78] cursor-pointer"
              >
                <option value="All">Client type: All</option>
                <option value="Enterprise">Enterprise</option>
                <option value="SME">SME</option>
                <option value="Partner">Partner</option>
              </select>
              <div className="pointer-events-none absolute right-2.5 top-2.5 text-slate-400 text-xs">
                ▾
              </div>
            </div>

            {/* Status Filter */}
            <div className="relative">
              <select
                value={selectedStatus}
                onChange={(e) => setSelectedStatus(e.target.value)}
                className="appearance-none bg-white border border-slate-200 rounded-lg pl-3.5 pr-8 py-2 text-sm text-slate-700 shadow-sm focus:outline-none focus:ring-1 focus:ring-[#007a78] cursor-pointer"
              >
                <option value="Active">Status: Active</option>
                <option value="All">Status: All</option>
                <option value="Inactive">Status: Inactive</option>
              </select>
              <div className="pointer-events-none absolute right-2.5 top-2.5 text-slate-400 text-xs">
                ▾
              </div>
            </div>
          </div>

          {/* ORGANISATIONS TABLE */}
          <div className="bg-white rounded-xl border border-slate-200/90 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/50">
                    <th className="py-3 px-6 text-[11px] font-semibold text-slate-400 tracking-wider uppercase">
                      ORGANISATION
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold text-slate-400 tracking-wider uppercase">
                      CLIENT TYPE
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold text-slate-400 tracking-wider uppercase">
                      PRIMARY CONTACT
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold text-slate-400 tracking-wider uppercase">
                      CASES
                    </th>
                    <th className="py-3 px-6 text-[11px] font-semibold text-slate-400 tracking-wider uppercase text-right">
                      {/* Actions */}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {isLoading ? (
                    <tr>
                      <td colSpan={5} className="py-12 text-center text-slate-400">
                        <div className="inline-block animate-spin rounded-full h-5 w-5 border-2 border-slate-300 border-t-[#007a78] mr-2 align-middle"></div>
                        Loading organisations...
                      </td>
                    </tr>
                  ) : filteredOrganisations.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-12 text-center text-slate-400">
                        No organisations found matching your filters.
                      </td>
                    </tr>
                  ) : (
                    filteredOrganisations.map((org) => (
                      <tr
                        key={org.id}
                        className="hover:bg-slate-50/80 transition-colors group"
                      >
                        {/* Organisation Name & Code/City */}
                        <td className="py-4 px-6">
                          <Link
                            href={`/organisations/${org.id}`}
                            className="font-medium text-slate-900 hover:text-[#007a78] transition-colors block"
                          >
                            {org.name}
                          </Link>
                          <div className="text-xs text-slate-400 mt-0.5">
                            {org.clientCode} · {org.city}
                          </div>
                        </td>

                        {/* Client Type */}
                        <td className="py-4 px-6 text-slate-700">
                          {org.clientType}
                        </td>

                        {/* Primary Contact */}
                        <td className="py-4 px-6 text-slate-700">
                          {org.primaryContact}
                        </td>

                        {/* Cases */}
                        <td className="py-4 px-6 text-slate-700">
                          {org.casesSummary}
                        </td>

                        {/* Edit Action */}
                        <td className="py-4 px-6 text-right">
                          <Link
                            href={`/organisations/${org.id}`}
                            className="text-[#007a78] hover:text-[#005f5d] font-medium text-sm inline-flex items-center gap-1 hover:underline cursor-pointer"
                          >
                            <span>Edit</span>
                            <span className="text-base leading-none">→</span>
                          </Link>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </main>
      </div>

      {/* ADD ORGANISATION MODAL */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full p-6 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <h2 className="text-lg font-bold text-slate-900">Add client organisation</h2>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg"
              >
                ✕
              </button>
            </div>

            {formError && (
              <div className="mt-4 p-3 rounded-lg bg-red-50 text-red-600 text-xs">
                {formError}
              </div>
            )}

            <form onSubmit={handleAddSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Organisation name *
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Ceylon Exports (Pvt) Ltd"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Client code
                  </label>
                  <input
                    type="text"
                    placeholder="CLI-0019"
                    value={formData.clientCode}
                    onChange={(e) => setFormData({ ...formData, clientCode: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Client type *
                  </label>
                  <select
                    value={formData.clientType}
                    onChange={(e) => setFormData({ ...formData, clientType: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  >
                    <option value="Enterprise">Enterprise</option>
                    <option value="SME">SME</option>
                    <option value="Partner">Partner</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Primary contact name *
                  </label>
                  <input
                    type="text"
                    required
                    placeholder="e.g. Nimali Perera"
                    value={formData.primaryContact}
                    onChange={(e) => setFormData({ ...formData, primaryContact: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Contact email *
                  </label>
                  <input
                    type="email"
                    required
                    placeholder="e.g. nimali@example.com"
                    value={formData.contactEmail}
                    onChange={(e) => setFormData({ ...formData, contactEmail: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Phone number
                  </label>
                  <input
                    type="text"
                    placeholder="+94 11 234 5678"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    City / Location
                  </label>
                  <input
                    type="text"
                    placeholder="e.g. Colombo"
                    value={formData.city}
                    onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                    className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Business registration number
                </label>
                <input
                  type="text"
                  placeholder="e.g. PV 123456"
                  value={formData.businessRegistrationNumber}
                  onChange={(e) => setFormData({ ...formData, businessRegistrationNumber: e.target.value })}
                  className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Business address
                </label>
                <input
                  type="text"
                  placeholder="e.g. 42 Marine Drive, Colombo 03"
                  value={formData.businessAddress}
                  onChange={(e) => setFormData({ ...formData, businessAddress: e.target.value })}
                  className="w-full bg-white border border-slate-200 rounded-lg px-3.5 py-2 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-[#007a78]"
                />
              </div>

              <div className="pt-4 flex items-center justify-end gap-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="px-4 py-2 rounded-lg border border-slate-200 text-sm font-medium text-slate-600 hover:bg-slate-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 rounded-lg bg-[#007a78] hover:bg-[#006866] text-white text-sm font-medium shadow-sm transition-colors disabled:opacity-50"
                >
                  {isSubmitting ? "Creating..." : "Create organisation"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
