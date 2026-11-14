"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

interface ChecklistTemplate {
  id: string;
  name: string;
  version: string;
  displayName: string;
  description?: string;
  requiredCount: number;
  optionalCount: number;
}

interface ClientType {
  id: string;
  name: string;
  description: string;
  defaultChecklistTemplateId?: string | null;
  defaultChecklistTemplateName?: string | null;
  defaultChecklistTemplateVersion?: string | null;
  defaultChecklistTemplateDisplayName?: string | null;
  requiredCount: number;
  optionalCount: number;
  createdAtUtc?: string;
  createdBy?: string;
}

const DEFAULT_TEMPLATES: ChecklistTemplate[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Enterprise onboarding",
    version: "v3",
    displayName: "Enterprise onboarding · v3",
    description: "Complete enterprise compliance, financial verification and multi-stage executive signoff.",
    requiredCount: 6,
    optionalCount: 2,
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    name: "SME onboarding",
    version: "v2",
    displayName: "SME onboarding · v2",
    description: "A streamlined verification pack designed for small and medium tier operations.",
    requiredCount: 4,
    optionalCount: 1,
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    name: "Partner onboarding",
    version: "v1",
    displayName: "Partner onboarding · v1",
    description: "Partner governance agreements and channel accreditation verification.",
    requiredCount: 5,
    optionalCount: 1,
  },
];

const DEFAULT_CLIENT_TYPES: ClientType[] = [
  {
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    name: "Enterprise",
    description: "Larger organisations with extended compliance requirements.",
    defaultChecklistTemplateId: "11111111-1111-1111-1111-111111111111",
    defaultChecklistTemplateName: "Enterprise onboarding",
    defaultChecklistTemplateVersion: "v3",
    defaultChecklistTemplateDisplayName: "Enterprise onboarding · v3",
    requiredCount: 6,
    optionalCount: 2,
  },
  {
    id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    name: "SME",
    description: "A focused checklist for small and medium businesses.",
    defaultChecklistTemplateId: "22222222-2222-2222-2222-222222222222",
    defaultChecklistTemplateName: "SME onboarding",
    defaultChecklistTemplateVersion: "v2",
    defaultChecklistTemplateDisplayName: "SME onboarding · v2",
    requiredCount: 4,
    optionalCount: 1,
  },
  {
    id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    name: "Partner",
    description: "Agreement-led onboarding for partner organisations.",
    defaultChecklistTemplateId: "33333333-3333-3333-3333-333333333333",
    defaultChecklistTemplateName: "Partner onboarding",
    defaultChecklistTemplateVersion: "v1",
    defaultChecklistTemplateDisplayName: "Partner onboarding · v1",
    requiredCount: 5,
    optionalCount: 1,
  },
];

export default function ClientTypesPage() {
  const [clientTypes, setClientTypes] = useState<ClientType[]>(DEFAULT_CLIENT_TYPES);
  const [templates, setTemplates] = useState<ChecklistTemplate[]>(DEFAULT_TEMPLATES);
  const [selectedClientType, setSelectedClientType] = useState<ClientType | null>(DEFAULT_CLIENT_TYPES[0]);
  const [isCreating, setIsCreating] = useState(false);

  // Form states
  const [formName, setFormName] = useState(DEFAULT_CLIENT_TYPES[0].name);
  const [formTemplateId, setFormTemplateId] = useState(DEFAULT_CLIENT_TYPES[0].defaultChecklistTemplateId || "");
  const [formDescription, setFormDescription] = useState(DEFAULT_CLIENT_TYPES[0].description);

  // Status & Feedback
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  useEffect(() => {
    let ignore = false;
    const fetchData = async () => {
      try {
        const [clientTypesRes, templatesRes] = await Promise.all([
          fetch(`${apiUrl}/api/client-types`),
          fetch(`${apiUrl}/api/client-types/templates`),
        ]);

        if (ignore) return;

        if (clientTypesRes.ok) {
          const typesData: ClientType[] = await clientTypesRes.json();
          if (typesData.length > 0 && !ignore) {
            setClientTypes(typesData);
            setSelectedClientType((prev) => {
              if (!prev || !typesData.some((t) => t.id === prev.id)) {
                setFormName(typesData[0].name);
                setFormTemplateId(typesData[0].defaultChecklistTemplateId || "");
                setFormDescription(typesData[0].description);
                return typesData[0];
              }
              return prev;
            });
          }
        }

        if (templatesRes.ok && !ignore) {
          const tplData: ChecklistTemplate[] = await templatesRes.json();
          if (tplData.length > 0) {
            setTemplates(tplData);
          }
        }
      } catch (err) {
        console.warn("Could not reach backend API, running with initial local data", err);
      }
    };

    void fetchData();
    return () => {
      ignore = true;
    };
  }, [apiUrl]);

  const handleEditClick = (clientType: ClientType) => {
    setIsCreating(false);
    setSelectedClientType(clientType);
    setFormName(clientType.name);
    setFormTemplateId(clientType.defaultChecklistTemplateId || "");
    setFormDescription(clientType.description);
    setErrorMessage(null);
    setSuccessMessage(null);

    // Scroll smoothly to form
    const formElement = document.getElementById("client-type-form");
    if (formElement) {
      formElement.scrollIntoView({ behavior: "smooth" });
    }
  };

  const handleCreateClick = () => {
    setIsCreating(true);
    setSelectedClientType(null);
    setFormName("");
    setFormDescription("");
    setFormTemplateId(templates[0]?.id || "");
    setErrorMessage(null);
    setSuccessMessage(null);

    const formElement = document.getElementById("client-type-form");
    if (formElement) {
      formElement.scrollIntoView({ behavior: "smooth" });
    }
  };

  const handleCancel = () => {
    setIsCreating(false);
    if (selectedClientType) {
      setFormName(selectedClientType.name);
      setFormTemplateId(selectedClientType.defaultChecklistTemplateId || "");
      setFormDescription(selectedClientType.description);
    } else if (clientTypes.length > 0) {
      setSelectedClientType(clientTypes[0]);
      setFormName(clientTypes[0].name);
      setFormTemplateId(clientTypes[0].defaultChecklistTemplateId || "");
      setFormDescription(clientTypes[0].description);
    }
    setErrorMessage(null);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName.trim()) {
      setErrorMessage("Type name is required.");
      return;
    }

    setSaving(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    const payload = {
      name: formName.trim(),
      description: formDescription.trim(),
      defaultChecklistTemplateId: formTemplateId || null,
    };

    try {
      if (isCreating) {
        const response = await fetch(`${apiUrl}/api/client-types`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          const errData = await response.json().catch(() => ({}));
          throw new Error(errData.message || "Failed to create client type");
        }

        const created: ClientType = await response.json();
        setClientTypes((prev) => [...prev, created]);
        setSelectedClientType(created);
        setIsCreating(false);
        setSuccessMessage(`Client type "${created.name}" created successfully.`);
      } else if (selectedClientType) {
        const response = await fetch(`${apiUrl}/api/client-types/${selectedClientType.id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          const errData = await response.json().catch(() => ({}));
          throw new Error(errData.message || "Failed to update client type");
        }

        const updated: ClientType = await response.json();
        setClientTypes((prev) =>
          prev.map((item) => (item.id === updated.id ? updated : item))
        );
        setSelectedClientType(updated);
        setSuccessMessage(`Client type "${updated.name}" updated successfully.`);
      }
    } catch {
      // Local optimistic fallback if offline
      const chosenTemplate = templates.find((t) => t.id === formTemplateId);
      if (isCreating) {
        const newLocal: ClientType = {
          id: `local-${Date.now()}`,
          name: formName.trim(),
          description: formDescription.trim(),
          defaultChecklistTemplateId: formTemplateId || null,
          defaultChecklistTemplateName: chosenTemplate?.name || null,
          defaultChecklistTemplateVersion: chosenTemplate?.version || null,
          defaultChecklistTemplateDisplayName: chosenTemplate
            ? `${chosenTemplate.name} · ${chosenTemplate.version}`
            : null,
          requiredCount: chosenTemplate?.requiredCount || 0,
          optionalCount: chosenTemplate?.optionalCount || 0,
        };
        setClientTypes((prev) => [...prev, newLocal]);
        setSelectedClientType(newLocal);
        setIsCreating(false);
        setSuccessMessage(`Client type "${newLocal.name}" saved.`);
      } else if (selectedClientType) {
        const updatedLocal: ClientType = {
          ...selectedClientType,
          name: formName.trim(),
          description: formDescription.trim(),
          defaultChecklistTemplateId: formTemplateId || null,
          defaultChecklistTemplateName: chosenTemplate?.name || null,
          defaultChecklistTemplateVersion: chosenTemplate?.version || null,
          defaultChecklistTemplateDisplayName: chosenTemplate
            ? `${chosenTemplate.name} · ${chosenTemplate.version}`
            : null,
          requiredCount: chosenTemplate?.requiredCount || 0,
          optionalCount: chosenTemplate?.optionalCount || 0,
        };
        setClientTypes((prev) =>
          prev.map((item) => (item.id === updatedLocal.id ? updatedLocal : item))
        );
        setSelectedClientType(updatedLocal);
        setSuccessMessage(`Client type "${updatedLocal.name}" saved.`);
      }
    } finally {
      setSaving(false);
      setTimeout(() => setSuccessMessage(null), 4000);
    }
  };

  return (
    <div className="min-h-screen bg-[#0c2429] flex flex-col md:flex-row text-slate-800">
      {/* ---------------- Sidebar Navigation (Matching Figma) ---------------- */}
      <aside className="w-full md:w-64 bg-[#0a2328] border-r border-[#14373d] flex flex-col justify-between shrink-0">
        <div>
          {/* Logo & Workspace header */}
          <div className="p-6 pb-2">
            <Link href="/" className="flex items-center gap-2">
              <span className="text-xl font-bold tracking-tight text-white">OnboardFlow</span>
            </Link>
            <div className="mt-2 text-[11px] font-semibold tracking-wider text-teal-400/80 uppercase">
              WORKSPACE
            </div>
          </div>

          {/* Nav Items */}
          <nav className="mt-4 px-3 space-y-1">
            <Link
              href="/dashboard"
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              Client organisations
            </Link>
            <Link
              href="/admin/client-types"
              className="flex items-center px-3 py-2 text-sm font-semibold text-white bg-[#0f6b64] rounded-xl shadow-xs"
            >
              Client types
            </Link>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              Checklist templates
            </a>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              Approval workflows
            </a>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              User accounts
            </a>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              Reviewer workload
            </a>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              Audit log
            </a>
            <a
              href="#"
              onClick={(e) => e.preventDefault()}
              className="flex items-center px-3 py-2 text-sm font-medium text-slate-300 hover:text-white hover:bg-white/5 rounded-xl transition"
            >
              System settings
            </a>
          </nav>
        </div>

        {/* Bottom profile / help section */}
        <div className="p-4 border-t border-[#14373d] text-xs text-slate-400 space-y-3">
          <div>
            <div className="font-medium text-slate-300">Need a hand?</div>
            <a href="#" onClick={(e) => e.preventDefault()} className="text-teal-300 hover:underline">
              Contact your administrator
            </a>
          </div>
          <div className="pt-2 border-t border-[#14373d]">
            <div className="font-semibold text-white text-sm">System administrator</div>
            <div className="text-slate-400 text-xs">Administrator</div>
          </div>
        </div>
      </aside>

      {/* ---------------- Main Content Area ---------------- */}
      <main className="flex-1 bg-[#f4f7f6] flex flex-col min-w-0">
        {/* Top Navbar */}
        <header className="h-14 px-8 border-b border-gray-200/80 bg-white flex items-center justify-between">
          <div className="flex items-center text-xs text-gray-500 font-medium space-x-1.5">
            <span className="text-gray-400">Workspace</span>
            <span>/</span>
            <span className="text-gray-800 font-semibold">Client types</span>
          </div>
          <div className="flex items-center space-x-6 text-xs text-gray-600 font-medium">
            <a href="#" onClick={(e) => e.preventDefault()} className="hover:text-gray-900 transition">
              Help centre
            </a>
            <div className="flex items-center gap-1 cursor-pointer hover:text-gray-900">
              <span>Notifications</span>
              <span className="bg-gray-100 text-gray-700 font-bold px-1.5 py-0.2 rounded-full text-[11px]">
                3
              </span>
            </div>
            <div className="h-7 w-7 rounded-full bg-teal-50 border border-teal-200 text-teal-800 flex items-center justify-center font-bold text-xs">
              KJ
            </div>
          </div>
        </header>

        {/* Content Container */}
        <div className="p-8 max-w-7xl w-full mx-auto space-y-8">
          {/* Header row with Title and Button */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
                Client types
              </h1>
              <p className="mt-1 text-sm text-gray-500">
                Link each client category to the right document checklist.
              </p>
            </div>
            <button
              onClick={handleCreateClick}
              className="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-lg text-sm font-semibold text-white bg-[#005f59] hover:bg-[#004b46] active:bg-[#003b37] shadow-xs transition"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              <span>Create client type</span>
            </button>
          </div>

          {/* Feedback messages */}
          {successMessage && (
            <div className="p-4 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl text-sm flex items-center gap-2">
              <svg className="w-5 h-5 text-emerald-600 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
              <span>{successMessage}</span>
            </div>
          )}

          {errorMessage && (
            <div className="p-4 bg-rose-50 border border-rose-200 text-rose-800 rounded-xl text-sm flex items-center gap-2">
              <svg className="w-5 h-5 text-rose-600 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <span>{errorMessage}</span>
            </div>
          )}

          {/* ---------------- Client Types Grid ---------------- */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {clientTypes.map((type) => {
              const isSelected = selectedClientType?.id === type.id && !isCreating;
              return (
                <div
                  key={type.id}
                  className={`bg-white rounded-2xl p-6 border transition flex flex-col justify-between shadow-xs ${
                    isSelected ? "border-[#005f59] ring-2 ring-[#005f59]/20" : "border-gray-200/90 hover:border-gray-300"
                  }`}
                >
                  <div>
                    <h3 className="text-xl font-bold text-gray-900">{type.name}</h3>
                    <p className="mt-2 text-xs text-gray-500 leading-relaxed min-h-[38px]">
                      {type.description || "No description provided."}
                    </p>

                    <div className="border-t border-gray-100 my-4 pt-3">
                      <div className="text-[10px] font-bold tracking-wider text-gray-400 uppercase">
                        CHECKLIST TEMPLATE
                      </div>
                      <div className="mt-1 text-sm font-semibold text-gray-900">
                        {type.defaultChecklistTemplateDisplayName ||
                          (type.defaultChecklistTemplateName
                            ? `${type.defaultChecklistTemplateName} · ${type.defaultChecklistTemplateVersion || "v1"}`
                            : "No checklist assigned")}
                      </div>
                      <div className="mt-1 text-xs text-gray-500">
                        {type.requiredCount} required · {type.optionalCount} optional
                      </div>
                    </div>
                  </div>

                  <div className="mt-5 pt-1">
                    <button
                      onClick={() => handleEditClick(type)}
                      className={`px-4 py-2 rounded-lg text-xs font-semibold border transition ${
                        isSelected
                          ? "bg-[#005f59] text-white border-[#005f59] hover:bg-[#004d48]"
                          : "bg-[#f2f4f4] hover:bg-[#e6e9e9] text-gray-700 border-gray-300/80"
                      }`}
                    >
                      Edit client type
                    </button>
                  </div>
                </div>
              );
            })}
          </div>

          {/* ---------------- Form Card (Edit / Create Client Type) ---------------- */}
          <div
            id="client-type-form"
            className="bg-white rounded-2xl p-7 border border-gray-200/90 shadow-xs"
          >
            <h2 className="text-lg font-bold text-gray-900">
              {isCreating ? "Create client type" : "Edit client type"}
            </h2>

            <form onSubmit={handleSave} className="mt-6 space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Type name input */}
                <div>
                  <label htmlFor="typeName" className="block text-xs font-semibold text-gray-700 mb-1.5">
                    Type name <span className="text-rose-500">*</span>
                  </label>
                  <input
                    id="typeName"
                    type="text"
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    placeholder="e.g. Enterprise, SME, Partner"
                    required
                    className="w-full px-3.5 py-2.5 text-sm text-gray-900 bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#005f59] focus:border-transparent transition"
                  />
                </div>

                {/* Default checklist dropdown */}
                <div>
                  <label htmlFor="defaultChecklist" className="block text-xs font-semibold text-gray-700 mb-1.5">
                    Default checklist <span className="text-rose-500">*</span>
                  </label>
                  <div className="relative">
                    <select
                      id="defaultChecklist"
                      value={formTemplateId}
                      onChange={(e) => setFormTemplateId(e.target.value)}
                      className="w-full px-3.5 py-2.5 text-sm text-gray-900 bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#005f59] focus:border-transparent appearance-none pr-10 transition cursor-pointer"
                    >
                      {templates.map((tpl) => (
                        <option key={tpl.id} value={tpl.id}>
                          {tpl.name} {tpl.version}
                        </option>
                      ))}
                    </select>
                    <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-gray-500">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </div>
                  </div>
                </div>
              </div>

              {/* Description field (optional for editing / creating) */}
              <div>
                <label htmlFor="typeDescription" className="block text-xs font-semibold text-gray-700 mb-1.5">
                  Description
                </label>
                <input
                  id="typeDescription"
                  type="text"
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="Summary of compliance and business requirements for this category"
                  className="w-full px-3.5 py-2.5 text-sm text-gray-900 bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#005f59] focus:border-transparent transition"
                />
              </div>

              {/* Notice Box (Matching Figma) */}
              <div className="bg-[#e6f4f1] border border-[#c2e5dc] rounded-xl p-4 text-emerald-900 flex flex-col space-y-0.5">
                <span className="text-xs font-bold text-[#0c6258]">
                  Applies to new cases
                </span>
                <span className="text-xs text-[#1e5852]">
                  Existing cases keep their current checklist.
                </span>
              </div>

              {/* Action buttons */}
              <div className="flex items-center gap-3 pt-1">
                <button
                  type="button"
                  onClick={handleCancel}
                  className="px-4 py-2 rounded-lg text-xs font-semibold text-gray-700 bg-[#f2f4f4] hover:bg-[#e6e9e9] border border-gray-300/80 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="px-5 py-2 rounded-lg text-xs font-semibold text-white bg-[#005f59] hover:bg-[#004b46] active:bg-[#003b37] disabled:opacity-50 shadow-xs transition"
                >
                  {saving ? "Saving..." : "Save changes"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </main>
    </div>
  );
}

