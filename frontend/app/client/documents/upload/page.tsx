"use client";

import React, { Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { RoleGuard } from "@/features/auth/components/RoleGuard";
import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";
import { DocumentUploadPanel } from "@/features/documents/components/DocumentUploadPanel";

function UploadPageContent() {
  const searchParams = useSearchParams();
  const caseId = searchParams.get("caseId") ?? "";
  const itemId = searchParams.get("itemId") ?? "";
  const itemName = searchParams.get("itemName") ?? undefined;

  if (!caseId || !itemId) {
    return (
      <div className="rounded-2xl bg-[#0d272d] border border-[#18434d] p-6 text-slate-200 max-w-xl">
        <p className="text-sm">
          Missing case or checklist item identifier. Please select a document
          from your checklist.
        </p>
        <Link
          href="/client/documents"
          className="inline-block mt-4 text-xs font-semibold text-cyan-400 hover:underline"
        >
          Back to Your documents
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <Link
        href="/client/documents"
        className="inline-block text-xs font-semibold text-cyan-700 hover:underline"
      >
        &larr; Back to Your documents
      </Link>
      <DocumentUploadPanel caseId={caseId} itemId={itemId} itemName={itemName} />
    </div>
  );
}

export default function ClientDocumentUploadPage() {
  return (
    <RoleGuard allowedRoles={["Client"]}>
      <WorkspaceLayout breadcrumb="Upload document">
        <div className="max-w-3xl mx-auto">
          <Suspense
            fallback={
              <p className="text-sm text-slate-500">Loading upload form...</p>
            }
          >
            <UploadPageContent />
          </Suspense>
        </div>
      </WorkspaceLayout>
    </RoleGuard>
  );
}
