"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";

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
        const token = localStorage.getItem("onboardflow_token");
        const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};

        const [clientTypesRes, templatesRes] = await Promise.all([
          fetch(`${apiUrl}/api/client-types`, { headers }),
          fetch(`${apiUrl}/api/client-types/templates`, { headers }),
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

    const token = localStorage.getItem("onboardflow_token");
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (token) headers["Authorization"] = `Bearer ${token}`;

    const payload = {
      name: formName.trim(),
      description: formDescription.trim(),
      defaultChecklistTemplateId: formTemplateId || null,
    };

    try {
      if (isCreating) {
        const response = await fetch(`${apiUrl}/api/client-types`, {
          method: "POST",
          headers,
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
          headers,
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
          id: crypto.randomUUID ? crypto.randomUUID() : `local-${Date.now()}`,
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

  const selectedTemplateForForm = templates.find((t) => t.id === formTemplateId);

  return (
    <WorkspaceLayout>
      <div className="p-8 max-w-7xl mx-auto space-y-6">
        {/* ---------------- Breadcrumbs & Page Header ---------------- */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-slate-200">
          <div>
            <div className="flex items-center gap-2 text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
              <span>Admin</span>
              <span>/</span>
              <span className="text-cyan-700 font-bold">Client types</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">
              Client types
            </h1>
            <p className="text-sm text-slate-600 mt-1">
              Configure business tiers and link each client category to its default document checklist template.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Link
              href="/admin/checklists"
              className="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-lg border border-slate-300 bg-white text-slate-700 text-xs font-semibold hover:bg-slate-50 hover:border-slate-400 transition shadow-2xs"
            >
              <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <span>Manage Checklist Templates</span>
            </Link>

            <button
              onClick={handleCreateClick}
              className="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-lg text-xs font-semibold text-white bg-[#0d272d] hover:bg-[#143d46] shadow-sm transition"
            >
              <svg className="w-4 h-4 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              <span>Create client type</span>
            </button>
          </div>
        </div>

        {/* ---------------- Feedback Messages ---------------- */}
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
                className={`bg-white rounded-2xl p-6 border transition flex flex-col justify-between shadow-2xs ${
                  isSelected ? "border-[#005f59] ring-2 ring-[#005f59]/20" : "border-slate-200/90 hover:border-slate-300"
                }`}
              >
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="text-lg font-bold text-slate-900">{type.name}</h3>
                    {isSelected && (
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-teal-50 text-teal-700 border border-teal-200 uppercase tracking-wide">
                        Selected
                      </span>
                    )}
                  </div>

                  <p className="mt-2 text-xs text-slate-500 leading-relaxed min-h-[38px]">
                    {type.description || "No description provided."}
                  </p>

                  <div className="border-t border-slate-100 my-4 pt-3">
                    <div className="text-[10px] font-bold tracking-wider text-slate-400 uppercase">
                      CHECKLIST TEMPLATE
                    </div>
                    <div className="mt-1 text-sm font-semibold text-slate-900">
                      {type.defaultChecklistTemplateDisplayName ||
                        (type.defaultChecklistTemplateName
                          ? `${type.defaultChecklistTemplateName} · ${type.defaultChecklistTemplateVersion || "v1"}`
                          : "No checklist assigned")}
                    </div>
                    <div className="mt-1 text-xs text-slate-500">
                      {type.requiredCount} required · {type.optionalCount} optional
                    </div>

                    {/* Direct Link into Checklist Builder */}
                    {type.defaultChecklistTemplateId && (
                      <div className="mt-2.5">
                        <Link
                          href={`/admin/checklist-templates/${type.defaultChecklistTemplateId}`}
                          className="inline-flex items-center gap-1.5 text-xs font-semibold text-cyan-700 hover:text-cyan-900 transition"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                          <span>Open Checklist Builder</span>
                          <span aria-hidden="true">→</span>
                        </Link>
                      </div>
                    )}
                  </div>
                </div>

                <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between gap-2">
                  <button
                    type="button"
                    onClick={() => handleEditClick(type)}
                    className={`px-3 py-1.5 rounded-lg text-xs font-semibold border transition ${
                      isSelected
                        ? "bg-[#0d272d] text-white border-[#0d272d] hover:bg-[#143d46]"
                        : "bg-slate-100 hover:bg-slate-200 text-slate-700 border-slate-200"
                    }`}
                  >
                    Edit client type
                  </button>

                  {type.defaultChecklistTemplateId && (
                    <Link
                      href={`/admin/checklist-templates/${type.defaultChecklistTemplateId}`}
                      className="px-3 py-1.5 rounded-lg text-xs font-semibold text-slate-600 bg-white hover:bg-slate-50 border border-slate-200 transition"
                    >
                      View items
                    </Link>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        {/* ---------------- Form Card (Edit / Create Client Type) ---------------- */}
        <div
          id="client-type-form"
          className="bg-white rounded-2xl p-7 border border-slate-200/90 shadow-2xs"
        >
          <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-6">
            <div>
              <h2 className="text-lg font-bold text-slate-900">
                {isCreating ? "Create client type" : "Edit client type"}
              </h2>
              <p className="text-xs text-slate-500 mt-0.5">
                {isCreating
                  ? "Define a new business category and select its document onboarding checklist."
                  : `Update properties and default checklist for "${formName}".`}
              </p>
            </div>
            {selectedTemplateForForm && (
              <Link
                href={`/admin/checklist-templates/${selectedTemplateForForm.id}`}
                className="hidden sm:inline-flex items-center gap-1.5 text-xs font-semibold text-cyan-700 hover:text-cyan-900 bg-cyan-50/70 border border-cyan-200 px-3 py-1.5 rounded-lg transition"
              >
                <span>Edit &quot;{selectedTemplateForForm.name}&quot; in Builder</span>
                <span aria-hidden="true">→</span>
              </Link>
            )}
          </div>

          <form onSubmit={handleSave} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Type name input */}
              <div>
                <label htmlFor="typeName" className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Type name <span className="text-rose-500">*</span>
                </label>
                <input
                  id="typeName"
                  type="text"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g. Enterprise, SME, Partner"
                  required
                  className="w-full px-3.5 py-2.5 text-sm text-slate-900 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:border-transparent transition"
                />
              </div>

              {/* Default checklist dropdown */}
              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <label htmlFor="defaultChecklist" className="block text-xs font-semibold text-slate-700">
                    Default checklist <span className="text-rose-500">*</span>
                  </label>
                  {formTemplateId && (
                    <Link
                      href={`/admin/checklist-templates/${formTemplateId}`}
                      className="text-[11px] font-semibold text-cyan-700 hover:text-cyan-900"
                    >
                      Open in Builder →
                    </Link>
                  )}
                </div>
                <div className="relative">
                  <select
                    id="defaultChecklist"
                    value={formTemplateId}
                    onChange={(e) => setFormTemplateId(e.target.value)}
                    className="w-full px-3.5 py-2.5 text-sm text-slate-900 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:border-transparent appearance-none pr-10 transition cursor-pointer"
                  >
                    {templates.map((tpl) => (
                      <option key={tpl.id} value={tpl.id}>
                        {tpl.name} {tpl.version} ({tpl.requiredCount || 0} req / {tpl.optionalCount || 0} opt)
                      </option>
                    ))}
                  </select>
                  <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-slate-500">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                  </div>
                </div>
              </div>
            </div>

            {/* Description field */}
            <div>
              <label htmlFor="typeDescription" className="block text-xs font-semibold text-slate-700 mb-1.5">
                Description
              </label>
              <input
                id="typeDescription"
                type="text"
                value={formDescription}
                onChange={(e) => setFormDescription(e.target.value)}
                placeholder="Summary of compliance and business requirements for this category"
                className="w-full px-3.5 py-2.5 text-sm text-slate-900 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600 focus:border-transparent transition"
              />
            </div>

            {/* Notice Box */}
            <div className="bg-[#e6f4f1] border border-[#c2e5dc] rounded-xl p-4 text-emerald-900 flex flex-col space-y-0.5">
              <span className="text-xs font-bold text-[#0c6258]">
                Applies to new cases
              </span>
              <span className="text-xs text-[#1e5852]">
                Existing cases keep their current checklist. Updating the default checklist assigns it to newly created onboarding cases of this type.
              </span>
            </div>

            {/* Action buttons */}
            <div className="flex items-center gap-3 pt-1">
              <button
                type="button"
                onClick={handleCancel}
                className="px-4 py-2 rounded-lg text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 border border-slate-300/80 transition"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={saving}
                className="px-5 py-2 rounded-lg text-xs font-semibold text-white bg-[#0d272d] hover:bg-[#143d46] active:bg-[#091b1f] disabled:opacity-50 shadow-2xs transition"
              >
                {saving ? "Saving..." : "Save changes"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </WorkspaceLayout>
  );
}
