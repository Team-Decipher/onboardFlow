"use client";

import { Suspense, useEffect, useState, use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { INITIAL_ORGANISATIONS } from "../page";
import {
  readOrganisationOverrides,
  saveOrganisationOverride,
} from "@/features/organisations/data";
import { OrganisationDetailsForm } from "@/features/organisations/components/OrganisationDetailsForm";
import type {
  ClientOrganisation,
  OrganisationAccessUser,
  OrganisationDetailsErrors,
  OrganisationDetailsField,
  OrganisationDetailsValues,
} from "@/features/organisations/types";

function toFormValues(organisation: ClientOrganisation): OrganisationDetailsValues {
  return {
    name: organisation.name || "",
    clientType: organisation.clientType || "",
    primaryContact: organisation.primaryContact || "",
    contactEmail: organisation.contactEmail || "",
    phoneNumber: organisation.phoneNumber || "",
    businessRegistrationNumber: organisation.businessRegistrationNumber || "",
    businessAddress: organisation.businessAddress || "",
  };
}

export default function OrganisationDetailsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return (
    <Suspense fallback={<div className="p-8 text-center text-sm text-slate-500">Loading organisation...</div>}>
      <OrganisationDetailsContent params={params} />
    </Suspense>
  );
}

function OrganisationDetailsContent({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const router = useRouter();
  const resolvedParams = use(params);
  const targetId = resolvedParams.id;

  const [activeTab, setActiveTab] = useState<"details" | "cases" | "access">("details");
  const [organisation, setOrganisation] = useState<ClientOrganisation | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Form state
  const [formData, setFormData] = useState<OrganisationDetailsValues>({
    name: "",
    clientType: "",
    primaryContact: "",
    contactEmail: "",
    phoneNumber: "",
    businessRegistrationNumber: "",
    businessAddress: "",
  });
  const [fieldErrors, setFieldErrors] = useState<OrganisationDetailsErrors>({});

  // Client access accounts state
  const [accessUsers, setAccessUsers] = useState<OrganisationAccessUser[]>([]);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  // Find or fetch organisation
  useEffect(() => {
    const fetchOrg = async () => {
      setIsLoading(true);
      try {
        const res = await fetch(`${apiUrl}/api/organisations/${targetId}`);
        if (res.ok) {
          const data: ClientOrganisation = await res.json();
          setOrganisation(data);
          setFormData(toFormValues(data));
          setIsLoading(false);
          return;
        }
      } catch {
        // Use the local demo data when the API is unavailable.
      }

      const localMatch = readOrganisationOverrides().find(
        (o) =>
          o.id.toLowerCase() === targetId.toLowerCase() ||
          o.clientCode.toLowerCase() === targetId.toLowerCase()
      );
      const matched = localMatch ?? INITIAL_ORGANISATIONS.find(
        (o) =>
          o.id.toLowerCase() === targetId.toLowerCase() ||
          o.clientCode.toLowerCase() === targetId.toLowerCase()
      );

      if (matched) {
        setOrganisation(matched);
        setFormData(toFormValues(matched));
      } else {
        setErrorMessage("This organisation could not be found.");
      }
      setIsLoading(false);
    };

    fetchOrg();
  }, [targetId, apiUrl]);

  // Fetch access users
  useEffect(() => {
    if (!organisation) return;

    const fetchAccess = async () => {
      try {
        const res = await fetch(`${apiUrl}/api/organisations/${organisation.id}/access`);
        if (res.ok) {
          const data = await res.json();
          setAccessUsers(data);
          return;
        }
      } catch {
        // Fallback
      }

      setAccessUsers([
        {
          id: "acc-1",
          fullName: organisation.primaryContact,
          email: organisation.contactEmail,
          role: "Primary Client Admin",
          status: "Active",
          lastLogin: "Today at 09:14 AM",
        },
        {
          id: "acc-2",
          fullName: `Finance Officer (${organisation.name})`,
          email: `finance@${organisation.contactEmail.includes("@") ? organisation.contactEmail.split("@")[1] : "client.com"}`,
          role: "Document Submitter",
          status: "Active",
          lastLogin: "Yesterday at 04:30 PM",
        },
      ]);
    };

    fetchAccess();
  }, [organisation, apiUrl]);

  // Handle Save Changes
  const handleSaveChanges = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!organisation) return;

    const errors: OrganisationDetailsErrors = {};
    if (!formData.name.trim()) errors.name = "Organisation name is required.";
    if (!formData.clientType.trim()) errors.clientType = "Select a client type.";
    if (!formData.primaryContact.trim()) errors.primaryContact = "Contact name is required.";
    if (!formData.contactEmail.trim()) {
      errors.contactEmail = "Contact email is required.";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.contactEmail.trim())) {
      errors.contactEmail = "Enter a valid email address.";
    }

    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      setErrorMessage("Please correct the highlighted fields.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);

    const updatePayload = {
      ...formData,
      clientCode: organisation.clientCode,
      city: organisation.city,
      casesSummary: organisation.casesSummary,
      status: organisation.status,
    };

    try {
      const res = await fetch(`${apiUrl}/api/organisations/${organisation.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(updatePayload),
      });

      if (res.ok) {
        const updatedOrg: ClientOrganisation = await res.json();
        setOrganisation(updatedOrg);
        setFormData(toFormValues(updatedOrg));
        showToast("Organisation details updated successfully.");
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.message || "Failed to update organisation.");
      }
    } catch {
      const updatedOrg: ClientOrganisation = {
        ...organisation,
        ...formData,
      };
      try {
        saveOrganisationOverride(updatedOrg);
        setOrganisation(updatedOrg);
        setFormData(toFormValues(updatedOrg));
        showToast("Changes saved on this device.");
      } catch {
        setErrorMessage("Could not reach the API or save changes on this device.");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const currentOrg =
    organisation ??
    INITIAL_ORGANISATIONS.find(
      (item) =>
        item.id.toLowerCase() === targetId.toLowerCase() ||
        item.clientCode.toLowerCase() === targetId.toLowerCase()
    ) ??
    INITIAL_ORGANISATIONS[0];

  const handleFieldChange = (field: OrganisationDetailsField, value: string) => {
    setFormData((previous) => ({ ...previous, [field]: value }));
    setFieldErrors((previous) => ({ ...previous, [field]: undefined }));
    setErrorMessage(null);
  };

  const handleCancel = () => {
    setFormData(toFormValues(currentOrg));
    setFieldErrors({});
    setErrorMessage(null);
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
            <Link href="/organisations" className="block">
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
            <Link
              href="/organisations"
              aria-current="page"
              className="flex items-center px-3 py-2 rounded-lg bg-[#0d7377] text-white transition-colors"
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
            <Link href="/organisations" className="hover:text-slate-600 transition-colors">
              Workspace
            </Link>
            <span className="text-slate-300">/</span>
            <span className="text-slate-700 font-semibold">Organisation details</span>
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
        <main className="p-8 max-w-5xl w-full">
          {isLoading && (
            <p role="status" className="mb-3 text-xs text-slate-500">
              Loading organisation details...
            </p>
          )}
          {/* Title & Organization Subtitle */}
          <div className="mb-6">
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">
              Organisation details
            </h1>
            <p className="text-sm text-slate-500 mt-1">
              {currentOrg.name} / {currentOrg.clientCode}
            </p>
          </div>

          {/* TABS */}
          <div role="tablist" aria-label="Organisation sections" className="flex items-center gap-8 border-b border-slate-200 mb-6 text-sm font-medium">
            <button
              type="button"
              role="tab"
              id="organisation-details-tab"
              aria-selected={activeTab === "details"}
              aria-controls="organisation-details-panel"
              onClick={() => setActiveTab("details")}
              className={`pb-3 transition-colors relative ${
                activeTab === "details"
                  ? "text-[#007a78] font-semibold"
                  : "text-slate-500 hover:text-slate-800"
              }`}
            >
              Organisation details
              {activeTab === "details" && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-[#007a78]" />
              )}
            </button>

            <button
              type="button"
              role="tab"
              id="linked-cases-tab"
              aria-selected={activeTab === "cases"}
              aria-controls="linked-cases-panel"
              onClick={() => setActiveTab("cases")}
              className={`pb-3 transition-colors relative ${
                activeTab === "cases"
                  ? "text-[#007a78] font-semibold"
                  : "text-slate-500 hover:text-slate-800"
              }`}
            >
              Linked cases
              {activeTab === "cases" && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-[#007a78]" />
              )}
            </button>

            <button
              type="button"
              role="tab"
              id="client-access-tab"
              aria-selected={activeTab === "access"}
              aria-controls="client-access-panel"
              onClick={() => setActiveTab("access")}
              className={`pb-3 transition-colors relative ${
                activeTab === "access"
                  ? "text-[#007a78] font-semibold"
                  : "text-slate-500 hover:text-slate-800"
              }`}
            >
              Client access
              {activeTab === "access" && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-[#007a78]" />
              )}
            </button>
          </div>

          {isLoading ? null : activeTab === "details" ? (
            <section
              id="organisation-details-panel"
              role="tabpanel"
              aria-labelledby="organisation-details-tab"
              tabIndex={0}
              className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm sm:p-6"
            >
              <h2 className="mb-4 text-sm font-semibold text-slate-900">
                Organisation information
              </h2>
              {errorMessage && (
                <p role="alert" className="mb-4 rounded-md bg-red-50 p-3 text-xs text-red-700">
                  {errorMessage}
                </p>
              )}
              <OrganisationDetailsForm
                values={formData}
                errors={fieldErrors}
                isSaving={isSaving}
                onChange={handleFieldChange}
                onSubmit={handleSaveChanges}
                onCancel={handleCancel}
              />
            </section>
          ) : null}

          {/* TAB 2: LINKED CASES */}
          {!isLoading && activeTab === "cases" && (
            <section id="linked-cases-panel" role="tabpanel" aria-labelledby="linked-cases-tab" tabIndex={0} className="bg-white rounded-xl border border-slate-200/90 shadow-sm p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-base font-bold text-slate-900">
                  Onboarding Cases for {currentOrg.name}
                </h2>
                <span className="text-xs font-semibold px-2.5 py-1 bg-emerald-50 text-emerald-700 rounded-full">
                  {currentOrg.casesSummary}
                </span>
              </div>

              {currentOrg.linkedCases && currentOrg.linkedCases.length > 0 ? (
                <div className="space-y-4">
                  {currentOrg.linkedCases.map((c) => (
                    <div
                      key={c.id}
                      className="p-5 border border-slate-200 rounded-lg bg-slate-50/50 flex flex-col md:flex-row md:items-center justify-between gap-4"
                    >
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-mono font-bold text-[#007a78] bg-teal-50 px-2 py-0.5 rounded">
                            {c.caseNumber}
                          </span>
                          <h3 className="font-semibold text-slate-900 text-sm">
                            {c.title}
                          </h3>
                        </div>
                        <div className="text-xs text-slate-500 flex items-center gap-3">
                          <span>Reviewer: <strong>{c.reviewerName}</strong></span>
                          <span>•</span>
                          <span>Target: {c.targetCompletionDate || "2 weeks"}</span>
                        </div>
                      </div>

                      <div className="flex items-center gap-6">
                        <div className="w-36">
                          <div className="flex justify-between text-xs text-slate-500 mb-1">
                            <span>Checklist</span>
                            <span className="font-medium text-slate-700">{c.progressPercentage}%</span>
                          </div>
                          <div className="w-full bg-slate-200 rounded-full h-1.5 overflow-hidden">
                            <div
                              className="bg-[#007a78] h-1.5 rounded-full"
                              style={{ width: `${c.progressPercentage}%` }}
                            />
                          </div>
                        </div>

                        <Link
                          href="/dashboard"
                          className="px-3 py-1.5 bg-white border border-slate-200 text-slate-700 hover:text-[#007a78] text-xs font-medium rounded-lg shadow-2xs hover:bg-slate-50 transition-colors"
                        >
                          View Case →
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-slate-400 text-sm py-8 text-center">
                  No active cases linked to this organisation.
                </p>
              )}
            </section>
          )}

          {/* TAB 3: CLIENT ACCESS */}
          {!isLoading && activeTab === "access" && (
            <section id="client-access-panel" role="tabpanel" aria-labelledby="client-access-tab" tabIndex={0} className="bg-white rounded-xl border border-slate-200/90 shadow-sm p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="text-base font-bold text-slate-900">Client Portal Access</h2>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Authorized client contacts who can upload documents and sign declarations.
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => showToast("Portal invitation sent.")}
                  className="bg-[#007a78] hover:bg-[#006866] text-white text-xs font-medium px-3 py-1.5 rounded-lg shadow-sm"
                >
                  + Invite contact
                </button>
              </div>

              <div className="divide-y divide-slate-100 border border-slate-100 rounded-lg">
                {accessUsers.map((user) => (
                  <div key={user.id} className="p-4 flex items-center justify-between">
                    <div>
                      <div className="font-medium text-sm text-slate-900">{user.fullName}</div>
                      <div className="text-xs text-slate-400 mt-0.5">{user.email}</div>
                    </div>
                    <div className="flex items-center gap-4">
                      <span className="text-xs font-medium text-slate-600 bg-slate-100 px-2 py-1 rounded">
                        {user.role}
                      </span>
                      <span className="text-xs text-emerald-600 font-semibold bg-emerald-50 px-2 py-0.5 rounded-full">
                        {user.status}
                      </span>
                      <button
                        type="button"
                        onClick={() => showToast(`Password reset link sent to ${user.email}`)}
                        className="text-xs text-[#007a78] hover:underline"
                      >
                        Reset login
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}
        </main>
      </div>
    </div>
  );
}
