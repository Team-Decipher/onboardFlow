"use client";

import React, { Suspense } from "react";
import { useParams } from "next/navigation";
import { OnboardingCaseForm } from "@/features/onboarding/components/OnboardingCaseForm";

function EditOnboardingCaseContent() {
  const params = useParams();
  const caseId = params?.id as string;
  return <OnboardingCaseForm mode="edit" caseId={caseId} />;
}

export default function EditOnboardingCasePage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#f3f6f7] flex items-center justify-center p-8 text-xs text-slate-500 font-medium">
          Loading Case Editor...
        </div>
      }
    >
      <EditOnboardingCaseContent />
    </Suspense>
  );
}
