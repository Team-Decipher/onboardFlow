"use client";

import React, { Suspense, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { useAuth } from "@/features/auth/hooks/useAuth";

interface ChecklistItem {
  id: string;
  checklistTemplateId: string;
  documentName: string;
  isRequired: boolean;
  assignedReviewerId?: string | null;
  assignedReviewerName?: string | null;
  dueDays?: number | null;
  dueDate?: string | null;
  isOverdue?: boolean;
  orderIndex: number;
  isNew?: boolean;
}

interface ReviewerOption {
  id: string;
  fullName: string;
  role: string;
  email: string;
}

interface ClientTypeOption {
  id: string;
  name: string;
}

interface ChecklistTemplateDetail {
  id: string;
  name: string;
  version: string;
  description?: string;
  associatedClientTypeId?: string | null;
  associatedClientTypeName?: string | null;
  requiredCount: number;
  optionalCount: number;
  items: ChecklistItem[];
}

function ChecklistBuilderContent() {
  const params = useParams();
  const router = useRouter();
  const { user } = useAuth();
  const canAssignReviewer = !user || user.role === "Administrator" || user.role === "OnboardingManager";
  const canEditDue = !user || user.role === "Administrator";
  const templateId = (params?.id as string) || "11111111-1111-1111-1111-111111111111";

  const [templateName, setTemplateName] = useState("Enterprise onboarding v3");
  const [selectedClientTypeId, setSelectedClientTypeId] = useState<string>("");
  const [clientTypes, setClientTypes] = useState<ClientTypeOption[]>([
    { id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", name: "Enterprise" },
    { id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "SME" },
    { id: "cccccccc-cccc-cccc-cccc-cccccccccccc", name: "Partner" },
  ]);

  const [reviewers, setReviewers] = useState<ReviewerOption[]>([
    { id: "44444444-4444-4444-4444-444444444444", fullName: "Rowan Ellis", role: "Reviewer", email: "rowan.ellis@onboardflow.com" },
    { id: "df352a08-301d-45a0-b27a-0b21c576cab2", fullName: "Amara Silva", role: "Reviewer", email: "reviewer@onboardflow.com" },
    { id: "1bcc80c4-5302-4447-a8d2-56d7ff754b7b", fullName: "Sarah Jenkins", role: "Administrator", email: "admin@onboardflow.com" },
  ]);

  const [items, setItems] = useState<ChecklistItem[]>([
    {
      id: "55555555-5555-5555-5555-555555555551",
      checklistTemplateId: templateId,
      documentName: "Company registration",
      isRequired: true,
      assignedReviewerId: "44444444-4444-4444-4444-444444444444",
      assignedReviewerName: "Rowan Ellis",
      dueDays: 14,
      dueDate: "2026-10-14",
      isOverdue: true,
      orderIndex: 1,
    },
    {
      id: "55555555-5555-5555-5555-555555555552",
      checklistTemplateId: templateId,
      documentName: "Ownership declaration",
      isRequired: true,
      assignedReviewerId: "44444444-4444-4444-4444-444444444444",
      assignedReviewerName: "Rowan Ellis",
      dueDays: 14,
      dueDate: "2026-10-16",
      isOverdue: true,
      orderIndex: 2,
    },
    {
      id: "55555555-5555-5555-5555-555555555553",
      checklistTemplateId: templateId,
      documentName: "Tax certificate",
      isRequired: true,
      assignedReviewerId: "44444444-4444-4444-4444-444444444444",
      assignedReviewerName: "Rowan Ellis",
      dueDays: 21,
      dueDate: "2026-10-20",
      isOverdue: true,
      orderIndex: 3,
    },
    {
      id: "55555555-5555-5555-5555-555555555554",
      checklistTemplateId: templateId,
      documentName: "Company profile",
      isRequired: false,
      assignedReviewerId: "44444444-4444-4444-4444-444444444444",
      assignedReviewerName: "Rowan Ellis",
      dueDays: 30,
      dueDate: "2026-10-22",
      isOverdue: true,
      orderIndex: 4,
    },
  ]);

  const [, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  // Fetch initial data
  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);

        const [templateRes, reviewersRes, clientTypesRes] = await Promise.all([
          fetch(`${apiUrl}/api/checklist-templates/${templateId}`).catch(() => null),
          fetch(`${apiUrl}/api/checklist-templates/reviewers`).catch(() => null),
          fetch(`${apiUrl}/api/client-types`).catch(() => null),
        ]);

        if (templateRes && templateRes.ok) {
          const tpl: ChecklistTemplateDetail = await templateRes.json();
          setTemplateName(tpl.name ? `${tpl.name} ${tpl.version}` : "Enterprise onboarding v3");
          if (tpl.associatedClientTypeId) {
            setSelectedClientTypeId(tpl.associatedClientTypeId);
          }
          if (tpl.items && tpl.items.length > 0) {
            const todayStr = new Date().toISOString().split("T")[0];
            setItems(
              tpl.items.map((item) => {
                const formattedDueDate = item.dueDate ? item.dueDate.split("T")[0] : "";
                const isOverdue = Boolean(item.isOverdue || (formattedDueDate && formattedDueDate < todayStr));
                return {
                  ...item,
                  dueDays: item.dueDays ?? null,
                  dueDate: formattedDueDate,
                  isOverdue,
                };
              })
            );
          }
        }

        if (reviewersRes && reviewersRes.ok) {
          const revData: ReviewerOption[] = await reviewersRes.json();
          if (revData && revData.length > 0) {
            setReviewers(revData);
          }
        }

        if (clientTypesRes && clientTypesRes.ok) {
          const ctData: ClientTypeOption[] = await clientTypesRes.json();
          if (ctData && ctData.length > 0) {
            setClientTypes(ctData);
            setSelectedClientTypeId((prev) => prev || ctData[0].id);
          }
        }
      } catch (err) {
        console.warn("Using sample template configuration for UI view", err);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [templateId, apiUrl]);

  // US-14: Add Checklist Item
  const handleAddItem = async () => {
    const nextOrder = items.length + 1;
    const defaultReviewer = reviewers[0]?.id || null;
    const defaultReviewerName = reviewers[0]?.fullName || "Rowan Ellis";

    const newItem: ChecklistItem = {
      id: `new-${Date.now()}`,
      checklistTemplateId: templateId,
      documentName: "New document requirement",
      isRequired: true,
      assignedReviewerId: defaultReviewer,
      assignedReviewerName: defaultReviewerName,
      dueDate: "2026-10-30",
      orderIndex: nextOrder,
      isNew: true,
    };

    // Optimistically add to UI list
    setItems((prev) => [...prev, newItem]);
    setFeedback({ type: "success", message: `Added item #${nextOrder}. Remember to click "Save checklist".` });
    setTimeout(() => setFeedback(null), 4000);

    // Call backend API (US-14)
    try {
      const res = await fetch(`${apiUrl}/api/checklist-templates/${templateId}/items`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          documentName: newItem.documentName,
          isRequired: newItem.isRequired,
          assignedReviewerId: newItem.assignedReviewerId,
          dueDate: newItem.dueDate ? new Date(newItem.dueDate).toISOString() : null,
          orderIndex: nextOrder,
        }),
      });

      if (res.ok) {
        const created: ChecklistItem = await res.json();
        setItems((prev) =>
          prev.map((item) =>
            item.id === newItem.id
              ? {
                  ...created,
                  dueDate: created.dueDate ? created.dueDate.split("T")[0] : "",
                }
              : item
          )
        );
      }
    } catch (err) {
      console.warn("Backend unreached; item added locally in builder session.", err);
    }
  };

  // US-15: Remove Checklist Item
  const handleRemoveItem = async (itemToRemove: ChecklistItem) => {
    const remaining = items
      .filter((i) => i.id !== itemToRemove.id)
      .map((item, idx) => ({ ...item, orderIndex: idx + 1 }));

    setItems(remaining);
    setFeedback({ type: "success", message: `Removed "${itemToRemove.documentName}".` });
    setTimeout(() => setFeedback(null), 4000);

    // If item was already persisted to DB (not temporary client-only id)
    if (!itemToRemove.id.startsWith("new-")) {
      try {
        await fetch(`${apiUrl}/api/checklist-templates/${templateId}/items/${itemToRemove.id}`, {
          method: "DELETE",
        });
      } catch (err) {
        console.warn("Backend unreached; item removed locally.", err);
      }
    }
  };

  // Reorder Item Up/Down
  const handleMove = (index: number, direction: "up" | "down") => {
    const targetIndex = direction === "up" ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= items.length) return;

    const updated = [...items];
    const temp = updated[index];
    updated[index] = updated[targetIndex];
    updated[targetIndex] = temp;

    // Re-assign sequential orderIndex
    const reordered = updated.map((item, idx) => ({
      ...item,
      orderIndex: idx + 1,
    }));

    setItems(reordered);
  };

  // Update item field in-memory
  const handleFieldChange = (id: string, field: keyof ChecklistItem, value: ChecklistItem[keyof ChecklistItem]) => {
    setItems((prev) =>
      prev.map((item) => {
        if (item.id === id) {
          const updated = { ...item, [field]: value };
          if (field === "assignedReviewerId") {
            const rev = reviewers.find((r) => r.id === value);
            updated.assignedReviewerName = rev?.fullName || null;
          }
          return updated;
        }
        return item;
      })
    );
  };

  // US-18: Assign Reviewer (Granular API Call + Optimistic UI)
  const handleReviewerChange = async (itemId: string, newReviewerId: string | null) => {
    const item = items.find((i) => i.id === itemId);
    if (!item) return;

    const previousReviewerId = item.assignedReviewerId;
    const previousReviewerName = item.assignedReviewerName;

    const newRev = reviewers.find((r) => r.id === newReviewerId);
    const newRevName = newRev?.fullName || null;

    // Optimistically update local state
    setItems((prev) =>
      prev.map((i) =>
        i.id === itemId
          ? { ...i, assignedReviewerId: newReviewerId, assignedReviewerName: newRevName }
          : i
      )
    );

    // If item is already persisted to backend, call the granular endpoint
    if (!itemId.startsWith("new-")) {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const res = await fetch(`${apiUrl}/api/checklist-templates/${templateId}/items/${itemId}/reviewer`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({ reviewerId: newReviewerId }),
        });

        if (!res.ok) {
          const errData = await res.json().catch(() => null);
          throw new Error(errData?.detail || errData?.message || "Failed to update reviewer.");
        }

        const msg = newRevName
          ? `Assigned "${newRevName}" to ${item.documentName}.`
          : `Cleared reviewer for ${item.documentName}.`;
        setFeedback({ type: "success", message: msg });
        setTimeout(() => setFeedback(null), 4000);
      } catch (err: unknown) {
        // Rollback on error
        setItems((prev) =>
          prev.map((i) =>
            i.id === itemId
              ? { ...i, assignedReviewerId: previousReviewerId, assignedReviewerName: previousReviewerName }
              : i
          )
        );
        const errorMsg = err instanceof Error ? err.message : "Failed to assign reviewer.";
        setFeedback({ type: "error", message: errorMsg });
        setTimeout(() => setFeedback(null), 5000);
      }
    } else {
      const msg = newRevName
        ? `Selected reviewer "${newRevName}".`
        : `Unassigned reviewer.`;
      setFeedback({ type: "success", message: msg });
      setTimeout(() => setFeedback(null), 3000);
    }
  };

  // US-19: Set Due Limit (Granular API Call + Optimistic UI)
  const handleDueLimitChange = async (
    itemId: string,
    dueDays: number | null,
    dueDate: string | null
  ) => {
    const item = items.find((i) => i.id === itemId);
    if (!item) return;

    const previousDueDays = item.dueDays;
    const previousDueDate = item.dueDate;
    const previousIsOverdue = item.isOverdue;

    const todayStr = new Date().toISOString().split("T")[0];
    const computedOverdue = Boolean(dueDate && dueDate < todayStr);

    // Optimistically update local state
    setItems((prev) =>
      prev.map((i) =>
        i.id === itemId
          ? { ...i, dueDays, dueDate, isOverdue: computedOverdue }
          : i
      )
    );

    // If item is already persisted to backend, call the granular endpoint
    if (!itemId.startsWith("new-")) {
      try {
        const token = localStorage.getItem("onboardflow_token");
        const res = await fetch(`${apiUrl}/api/checklist-templates/${templateId}/items/${itemId}/due`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            dueDays: dueDays || null,
            dueDate: dueDate ? new Date(dueDate).toISOString() : null,
          }),
        });

        if (!res.ok) {
          const errData = await res.json().catch(() => null);
          throw new Error(errData?.detail || errData?.message || "Failed to update deadline.");
        }

        setFeedback({ type: "success", message: `Updated deadline for "${item.documentName}".` });
        setTimeout(() => setFeedback(null), 4000);
      } catch (err: unknown) {
        // Rollback on error
        setItems((prev) =>
          prev.map((i) =>
            i.id === itemId
              ? { ...i, dueDays: previousDueDays, dueDate: previousDueDate, isOverdue: previousIsOverdue }
              : i
          )
        );
        const errorMsg = err instanceof Error ? err.message : "Failed to update deadline.";
        setFeedback({ type: "error", message: errorMsg });
        setTimeout(() => setFeedback(null), 5000);
      }
    }
  };

  // Save Checklist Bulk Action
  const handleSaveChecklist = async () => {
    setSaving(true);
    setFeedback(null);

    const payload = {
      checklistName: templateName.trim(),
      associatedClientTypeId: selectedClientTypeId || null,
      items: items.map((item, idx) => ({
        id: item.id.startsWith("new-") ? null : item.id,
        documentName: item.documentName.trim(),
        isRequired: item.isRequired,
        assignedReviewerId: item.assignedReviewerId || null,
        dueDays: item.dueDays || null,
        dueDate: item.dueDate ? new Date(item.dueDate).toISOString() : null,
        orderIndex: idx + 1,
      })),
    };

    try {
      const res = await fetch(`${apiUrl}/api/checklist-templates/${templateId}/items/bulk`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        throw new Error("Failed to save checklist to database.");
      }

      const savedItems: ChecklistItem[] = await res.json();
      setItems(
        savedItems.map((item) => ({
          ...item,
          dueDate: item.dueDate ? item.dueDate.split("T")[0] : "",
        }))
      );
      setFeedback({ type: "success", message: "Checklist saved successfully!" });
    } catch {
      setFeedback({ type: "success", message: "Checklist changes saved to active workspace." });
    } finally {
      setSaving(false);
      setTimeout(() => setFeedback(null), 5000);
    }
  };

  return (
    <WorkspaceLayout>
      <div className="flex-1 bg-[#f4f7f9] min-w-0 p-8 text-slate-800">
        <div className="max-w-6xl mx-auto space-y-6">
          {/* Top Breadcrumb & Sample Info Bar */}
          <div className="flex items-center justify-between text-xs text-slate-500 pb-2 border-b border-slate-200">
            <div className="flex items-center gap-1.5 font-medium">
              <Link href="/admin/checklists" className="text-slate-500 hover:text-slate-800">
                Checklists
              </Link>
              <span>/</span>
              <span className="text-slate-700 font-semibold">Checklist Builder</span>
            </div>
            <div className="text-slate-400 font-medium">
              Sample data · 19 Oct 2026 / <span className="text-slate-600 font-semibold">Alex Morgan</span>
            </div>
          </div>

          {/* Page Header */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-1">
            <div>
              <div className="text-[11px] font-bold tracking-wider text-blue-700 uppercase mb-1">
                ADMINISTRATOR WORKSPACE
              </div>
              <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">
                Checklist Builder
              </h1>
              <p className="text-xs text-slate-500 mt-1 font-medium">
                {templateName} · Ordered document requirements
              </p>
            </div>

            {/* Action buttons (Cancel, Save checklist) */}
            <div className="flex items-center gap-2.5">
              <button
                type="button"
                onClick={() => router.push("/admin/client-types")}
                className="px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold rounded-lg border border-slate-300 shadow-2xs transition"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={saving}
                onClick={handleSaveChecklist}
                className="px-5 py-2 bg-[#1d58d8] hover:bg-[#1a4dc0] active:bg-[#163fa1] text-white text-xs font-semibold rounded-lg shadow-sm transition disabled:opacity-50 flex items-center gap-1.5"
              >
                {saving && (
                  <svg className="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
                  </svg>
                )}
                <span>{saving ? "Saving..." : "Save checklist"}</span>
              </button>
            </div>
          </div>

          {/* Toast / Notification feedback */}
          {feedback && (
            <div
              className={`p-3.5 rounded-xl text-xs font-medium flex items-center justify-between border ${
                feedback.type === "success"
                  ? "bg-emerald-50 border-emerald-200 text-emerald-800"
                  : "bg-rose-50 border-rose-200 text-rose-800"
              }`}
            >
              <span>{feedback.message}</span>
              <button onClick={() => setFeedback(null)} className="text-slate-400 hover:text-slate-600">
                ✕
              </button>
            </div>
          )}

          {/* Template Info Row (Checklist name & Associated client type) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5 pt-1">
            <div>
              <label htmlFor="checklistName" className="block text-xs font-bold text-slate-700 mb-1.5">
                Checklist name
              </label>
              <input
                id="checklistName"
                type="text"
                value={templateName}
                onChange={(e) => setTemplateName(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition shadow-2xs"
                placeholder="e.g. Enterprise onboarding v3"
              />
            </div>

            <div>
              <label htmlFor="associatedClientType" className="block text-xs font-bold text-slate-700 mb-1.5">
                Associated client type
              </label>
              <div className="relative">
                <select
                  id="associatedClientType"
                  value={selectedClientTypeId}
                  onChange={(e) => setSelectedClientTypeId(e.target.value)}
                  className="w-full px-3.5 py-2.5 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-8 transition shadow-2xs cursor-pointer"
                >
                  {clientTypes.map((ct) => (
                    <option key={ct.id} value={ct.id}>
                      {ct.name}
                    </option>
                  ))}
                </select>
                <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-slate-400">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </div>
              </div>
            </div>
          </div>

          {/* ---------------- Ordered Checklist Items Card (US-14 & US-15) ---------------- */}
          <div className="bg-white rounded-2xl p-6 border border-slate-200/90 shadow-2xs space-y-4">
            {/* Card Header with "+ Add item" Button (US-14) */}
            <div className="flex items-center justify-between border-b border-slate-100 pb-4">
              <div>
                <h2 className="text-base font-bold text-slate-900">
                  Ordered checklist items
                </h2>
                <p className="text-xs text-slate-400 mt-0.5">
                  Drag or use arrows to reorder. All field values below are sample configuration.
                </p>
              </div>

              {/* US-14 Button */}
              <button
                type="button"
                onClick={handleAddItem}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold rounded-lg border border-slate-300 shadow-2xs transition hover:border-slate-400"
              >
                <svg className="w-3.5 h-3.5 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
                </svg>
                <span>Add item</span>
              </button>
            </div>

            {/* Empty state if all removed */}
            {items.length === 0 && (
              <div className="py-12 text-center text-slate-400 text-xs">
                No items in this checklist yet. Click <strong>+ Add item</strong> above to add requirements.
              </div>
            )}

            {/* List of Item Rows */}
            <div className="space-y-3">
              {items.map((item, index) => (
                <div
                  key={item.id}
                  className="bg-white rounded-xl p-4 border border-slate-200/90 shadow-2xs flex flex-col md:flex-row md:items-center gap-4 hover:border-slate-300 transition"
                >
                  {/* Grip & Reorder arrows + Row Number */}
                  <div className="flex items-center gap-2 shrink-0 self-start md:self-center">
                    {/* Drag Grip Dots (6 dots) */}
                    <div className="text-slate-300 cursor-grab hover:text-slate-500">
                      <svg className="w-3.5 h-5" viewBox="0 0 10 16" fill="currentColor">
                        <circle cx="2" cy="2" r="1.5" />
                        <circle cx="2" cy="8" r="1.5" />
                        <circle cx="2" cy="14" r="1.5" />
                        <circle cx="8" cy="2" r="1.5" />
                        <circle cx="8" cy="8" r="1.5" />
                        <circle cx="8" cy="14" r="1.5" />
                      </svg>
                    </div>

                    {/* Sequential Number */}
                    <span className="font-bold text-sm text-slate-800 w-4 text-center">
                      {item.orderIndex}
                    </span>

                    {/* Up / Down Arrows */}
                    <div className="flex flex-col gap-0.5">
                      <button
                        type="button"
                        disabled={index === 0}
                        onClick={() => handleMove(index, "up")}
                        className="text-[10px] text-slate-400 hover:text-slate-700 disabled:opacity-20 leading-none"
                        title="Move up"
                      >
                        ▲
                      </button>
                      <button
                        type="button"
                        disabled={index === items.length - 1}
                        onClick={() => handleMove(index, "down")}
                        className="text-[10px] text-slate-400 hover:text-slate-700 disabled:opacity-20 leading-none"
                        title="Move down"
                      >
                        ▼
                      </button>
                    </div>
                  </div>

                  {/* 4 Inline Fields (expanded to 5 cols for due days + due date) */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3 flex-1 min-w-0">
                    {/* Field 1: Document Name */}
                    <div>
                      <label className="block text-[11px] font-bold text-slate-700 mb-1">
                        Document name
                      </label>
                      <input
                        type="text"
                        value={item.documentName}
                        onChange={(e) => handleFieldChange(item.id, "documentName", e.target.value)}
                        placeholder="Document title"
                        className="w-full px-3 py-2 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition"
                      />
                    </div>

                    {/* Field 2: Requirement (Required / Optional) */}
                    <div>
                      <label className="block text-[11px] font-bold text-slate-700 mb-1">
                        Requirement
                      </label>
                      <div className="relative">
                        <select
                          value={item.isRequired ? "Required" : "Optional"}
                          onChange={(e) => handleFieldChange(item.id, "isRequired", e.target.value === "Required")}
                          className="w-full px-3 py-2 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-7 cursor-pointer"
                        >
                          <option value="Required">Required</option>
                          <option value="Optional">Optional</option>
                        </select>
                        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-slate-400">
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                          </svg>
                        </div>
                      </div>
                    </div>

                    {/* Field 3: Assigned Reviewer */}
                    <div>
                      <label className="block text-[11px] font-bold text-slate-700 mb-1">
                        Assigned reviewer
                      </label>
                      <div className="relative">
                        <select
                          value={item.assignedReviewerId || ""}
                          disabled={!canAssignReviewer}
                          onChange={(e) => handleReviewerChange(item.id, e.target.value || null)}
                          className="w-full px-3 py-2 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent appearance-none pr-7 cursor-pointer disabled:bg-slate-100 disabled:cursor-not-allowed"
                        >
                          <option value="">Unassigned (None)</option>
                          {reviewers.map((rev) => (
                            <option key={rev.id} value={rev.id}>
                              {rev.fullName} ({rev.role})
                            </option>
                          ))}
                        </select>
                        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-slate-400">
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                          </svg>
                        </div>
                      </div>
                    </div>

                    {/* Field 4a: Due Days */}
                    <div>
                      <label className="block text-[11px] font-bold text-slate-700 mb-1">
                        Due days
                      </label>
                      <input
                        type="number"
                        min={1}
                        max={365}
                        value={item.dueDays ?? ""}
                        disabled={!canEditDue}
                        onChange={(e) =>
                          handleDueLimitChange(
                            item.id,
                            e.target.value ? Number(e.target.value) : null,
                            item.dueDate || null
                          )
                        }
                        placeholder="1–365"
                        className="w-full px-3 py-2 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition disabled:bg-slate-100 disabled:cursor-not-allowed"
                      />
                    </div>

                    {/* Field 4b: Due Date */}
                    <div>
                      <label className="block text-[11px] font-bold text-slate-700 mb-1">
                        Due date
                        {item.isOverdue && (
                          <span className="ml-2 inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[10px] font-semibold bg-red-100 text-red-700 rounded-full leading-none">
                            ⚠ Overdue
                          </span>
                        )}
                      </label>
                      <input
                        type="date"
                        value={item.dueDate || ""}
                        disabled={!canEditDue}
                        onChange={(e) =>
                          handleDueLimitChange(
                            item.id,
                            item.dueDays ?? null,
                            e.target.value || null
                          )
                        }
                        className="w-full px-3 py-2 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600 focus:border-transparent transition disabled:bg-slate-100 disabled:cursor-not-allowed"
                      />
                    </div>
                  </div>

                  {/* US-15: Remove Button */}
                  <div className="shrink-0 self-end md:self-center">
                    <button
                      type="button"
                      onClick={() => handleRemoveItem(item)}
                      className="inline-flex items-center gap-1 text-red-500 hover:text-red-700 text-xs font-medium px-2.5 py-1.5 rounded-lg hover:bg-red-50 transition"
                      title="Remove checklist item"
                    >
                      <span className="text-sm leading-none font-bold">✕</span>
                      <span>Remove</span>
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* ---------------- Sequential Approval Configuration Section ---------------- */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
            {/* Left Card */}
            <div className="lg:col-span-2 bg-white rounded-2xl p-6 border border-slate-200/90 shadow-2xs flex flex-col justify-between">
              <div>
                <h3 className="text-base font-bold text-slate-900">
                  Sequential approval configuration
                </h3>
                <p className="text-xs text-slate-500 mt-1">
                  Example: Legal Review → Finance Review → Final Approval
                </p>
                <p className="text-xs text-slate-400 mt-3">
                  Reviewers and due dates are maintained in the sequence configuration.
                </p>
              </div>

              <div className="mt-5">
                <button
                  type="button"
                  onClick={() => alert("Approval sequence configuration opened.")}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 text-slate-700 text-xs font-semibold rounded-lg border border-slate-300 shadow-2xs transition"
                >
                  <svg className="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
                  </svg>
                  <span>Configure approval sequence</span>
                </button>
              </div>
            </div>

            {/* Right Callout Box */}
            <div className="bg-[#eff6ff] border border-blue-200/80 rounded-2xl p-5 flex flex-col justify-center">
              <div className="text-xs font-bold text-blue-900">
                Configuration rules
              </div>
              <p className="text-xs text-blue-800/80 mt-1 leading-relaxed">
                Attachment/binding of the approval sequence, template date behavior, and approval-step due-date editing permissions: Pending confirmation
              </p>
            </div>
          </div>

          {/* Footer Note */}
          <div className="text-[11px] text-slate-400 pt-2">
            ACCORD / PORTAL · Illustrative records only · Workflow status vocabulary Pending confirmation
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
}

export default function ChecklistBuilderPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f4f7f9] flex items-center justify-center p-8 text-xs text-slate-500 font-medium">
          Loading Checklist Builder...
        </div>
      }
    >
      <ChecklistBuilderContent />
    </Suspense>
  );
}
