"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { getInitials } from "@/features/auth/hooks/useAuth";

interface WorkflowStep {
  id: string;
  title: string;
  status: string;
  order: number;
  description: string;
  completedAt?: string | null;
  documents: string[];
}

interface WorkflowData {
  caseId: string;
  clientName: string;
  headline: string;
  subtitle: string;
  cardTitle: string;
  category: string;
  progressPercentage: number;
  steps: WorkflowStep[];
}

interface UserSession {
  fullName: string;
  email: string;
  role: string;
  company: string;
}

const DEFAULT_WORKFLOW: WorkflowData = {
  caseId: "OBF-2026-904",
  clientName: "Apex Global Advisory Partners",
  headline: "Great partnerships\nstart here.",
  subtitle: "One place for your clients, documents and every step in between.",
  cardTitle: "A smoother start",
  category: "CLIENT ONBOARDING & DOCUMENT WORKFLOW",
  progressPercentage: 67,
  steps: [
    {
      id: "step-1",
      title: "Create your case",
      status: "Approved",
      order: 1,
      description: "Initial client KYC profile and case registration established.",
      completedAt: "2026-10-30T10:04:15.306Z",
      documents: ["Client_Profile_Form.pdf", "KYC_Verification.pdf"],
    },
    {
      id: "step-2",
      title: "Collect documents",
      status: "Approved",
      order: 2,
      description: "Required compliance paperwork, certificate of incorporation, and tax IDs uploaded.",
      completedAt: "2026-10-31T10:04:15.306Z",
      documents: [
        "Certificate_of_Incorporation.pdf",
        "Tax_Identification_W8BEN.pdf",
        "Signatory_Resolution.pdf",
      ],
    },
    {
      id: "step-3",
      title: "Review & approve",
      status: "In review",
      order: 3,
      description: "Legal counsel and compliance officers review submitted documents for final clearance.",
      completedAt: null,
      documents: ["Compliance_Audit_Checklist.pdf"],
    },
  ],
};

export default function DashboardPage() {
  const router = useRouter();
  const [data, setData] = useState<WorkflowData>(DEFAULT_WORKFLOW);
  const [user, setUser] = useState<UserSession | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeStep, setActiveStep] = useState<WorkflowStep | null>(null);
  const [isBackendConnected, setIsBackendConnected] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const fetchWorkflow = useCallback(async () => {
    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/onboarding/workflow`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const json: WorkflowData = await res.json();
        setData(json);
        setIsBackendConnected(true);
      } else {
        setIsBackendConnected(false);
      }
    } catch {
      setIsBackendConnected(false);
    }
  }, [apiUrl]);

  useEffect(() => {
    let ignore = false;

    async function init() {
      try {
        const storedUser = localStorage.getItem("onboardflow_user");
        if (storedUser && !ignore) {
          setUser(JSON.parse(storedUser));
        }
      } catch {
        // ignore
      }

      try {
        const token = localStorage.getItem("onboardflow_token");
        const res = await fetch(`${apiUrl}/api/onboarding/workflow`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!ignore) {
          if (res.ok) {
            const json: WorkflowData = await res.json();
            setData(json);
            setIsBackendConnected(true);
          } else {
            setIsBackendConnected(false);
          }
        }
      } catch {
        if (!ignore) {
          setIsBackendConnected(false);
        }
      }
    }

    init();

    return () => {
      ignore = true;
    };
  }, [apiUrl]);

  const handleLogout = () => {
    localStorage.removeItem("onboardflow_token");
    localStorage.removeItem("onboardflow_user");
    document.cookie = "onboardflow_token=; path=/; max-age=0";
    document.cookie = "onboardflow_role=; path=/; max-age=0";
    router.push("/login");
  };

  const handleToggleStep = async (stepId: string) => {
    setIsLoading(true);
    setStatusMessage(null);

    // Optimistic UI update
    setData((prev) => {
      const updatedSteps = prev.steps.map((s) => {
        if (s.id === stepId) {
          const nextStatus = s.status.toLowerCase() === "approved" ? "In review" : "Approved";
          return { ...s, status: nextStatus };
        }
        return s;
      });
      const approvedCount = updatedSteps.filter((s) => s.status.toLowerCase() === "approved").length;
      const progress = Math.round((approvedCount / updatedSteps.length) * 100);
      return {
        ...prev,
        steps: updatedSteps,
        progressPercentage: progress,
      };
    });

    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/onboarding/steps/${stepId}/toggle`, {
        method: "POST",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const updated: WorkflowData = await res.json();
        setData(updated);
        setIsBackendConnected(true);
        setStatusMessage("Step status updated via backend API.");
      } else if (res.status === 403) {
        setStatusMessage("403 Forbidden: You do not have permission to review this step.");
        fetchWorkflow();
      }
    } catch {
      setStatusMessage("Updated locally (offline mode).");
    } finally {
      setIsLoading(false);
      setTimeout(() => setStatusMessage(null), 3500);
    }
  };

  const handleReset = async () => {
    setIsLoading(true);
    try {
      const token = localStorage.getItem("onboardflow_token");
      const res = await fetch(`${apiUrl}/api/onboarding/reset`, {
        method: "POST",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const resetData: WorkflowData = await res.json();
        setData(resetData);
        setIsBackendConnected(true);
        setStatusMessage("Workflow reset to original state.");
      } else {
        setData(DEFAULT_WORKFLOW);
      }
    } catch {
      setData(DEFAULT_WORKFLOW);
    } finally {
      setIsLoading(false);
      setTimeout(() => setStatusMessage(null), 3500);
    }
  };

  return (
    <div className="min-h-screen bg-[#0d272d] text-white flex flex-col justify-between px-6 py-10 sm:px-12 lg:px-20 font-sans selection:bg-[#00796b] selection:text-white">
      {/* Top Header */}
      <header className="w-full max-w-5xl mx-auto flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="text-2xl sm:text-3xl font-bold tracking-tight text-white">
            OnboardFlow
          </span>
          <span
            className={`w-2 h-2 rounded-full ${
              isBackendConnected ? "bg-emerald-400" : "bg-amber-400"
            }`}
            title={isBackendConnected ? "API Connected" : "Local / Offline"}
          />
        </div>

        {/* User Session & Logout Header Controls */}
        <div className="flex items-center gap-3">
          {user && (
            <div className="flex items-center gap-2 bg-[#12363e]/70 border border-[#1b4650] px-3 py-1.5 rounded-full text-xs text-[#a0bac0]">
              <div className="w-5 h-5 rounded-full bg-[#0b7c74] text-white flex items-center justify-center font-bold text-[10px]">
                {getInitials(user.fullName)}
              </div>
              <span className="font-medium text-white">{user.fullName}</span>
              <span className="text-[#6d949c]">({user.role})</span>
            </div>
          )}

          {user?.role === "Administrator" && (
            <Link
              href="/admin/users"
              className="text-xs bg-[#0b7c74] hover:bg-[#096963] text-white font-medium px-3.5 py-1.5 rounded-full transition-colors shadow-xs"
            >
              User accounts →
            </Link>
          )}

          <button
            type="button"
            onClick={handleLogout}
            className="text-xs bg-[#173e46] hover:bg-[#20515b] text-[#c2dce0] hover:text-white px-3 py-1.5 rounded-full border border-[#235864] transition-colors"
            title="Log out and return to Login Screen"
          >
            ← Sign Out
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="w-full max-w-xl mx-auto my-auto flex flex-col pt-12 pb-16">
        <div className="mb-10 text-left">
          <h1 className="text-4xl sm:text-5xl lg:text-[54px] font-extrabold tracking-tight text-white leading-[1.12]">
            Great partnerships
            <br />
            start here.
          </h1>
          <p className="mt-5 text-lg sm:text-xl text-[#92adb2] font-normal leading-relaxed max-w-lg">
            One place for your clients, documents and every step in between.
          </p>
        </div>

        {/* Central Card */}
        <div className="bg-white text-slate-900 rounded-[22px] p-7 sm:p-9 shadow-2xl shadow-black/40 transition-all duration-300">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-[#162329]">
              {data.cardTitle}
            </h2>
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              {data.caseId}
            </span>
          </div>

          {/* Workflow Steps List */}
          <div className="space-y-4">
            {data.steps.map((step) => {
              const isApproved = step.status.toLowerCase() === "approved";
              const isInReview = step.status.toLowerCase() === "in review";

              return (
                <div
                  key={step.id}
                  onClick={() => setActiveStep(activeStep?.id === step.id ? null : step)}
                  className={`group flex items-center justify-between p-4 rounded-2xl border transition-all duration-200 cursor-pointer ${
                    activeStep?.id === step.id
                      ? "border-[#0b7c74] bg-[#f0f8f8]"
                      : "border-slate-100 bg-[#fbfdfd] hover:border-slate-200 hover:bg-slate-50/70"
                  }`}
                >
                  <div className="flex items-center gap-3.5">
                    <button
                      type="button"
                      disabled={isLoading}
                      onClick={(e) => {
                        e.stopPropagation();
                        handleToggleStep(step.id);
                      }}
                      className={`w-6 h-6 rounded-full flex items-center justify-center transition-all ${
                        isApproved
                          ? "bg-[#cbf1db] text-[#167448] hover:scale-110"
                          : isInReview
                          ? "bg-[#d8edf9] text-[#1c6fa6] hover:scale-110"
                          : "bg-slate-200 text-slate-500 hover:scale-110"
                      }`}
                      title="Toggle approval state"
                    >
                      {isApproved ? (
                        <svg className="w-3.5 h-3.5 fill-current" viewBox="0 0 20 20">
                          <path
                            fillRule="evenodd"
                            d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                            clipRule="evenodd"
                          />
                        </svg>
                      ) : (
                        <span className="w-2 h-2 rounded-full bg-current opacity-70" />
                      )}
                    </button>

                    <span
                      className={`text-base sm:text-[17px] font-medium tracking-tight ${
                        isApproved ? "text-slate-800" : "text-slate-900"
                      }`}
                    >
                      {step.title}
                    </span>
                  </div>

                  <span
                    className={`text-xs px-3 py-1 rounded-full font-medium transition-colors ${
                      isApproved
                        ? "bg-[#e2f7eb] text-[#137343]"
                        : isInReview
                        ? "bg-[#e3f2fd] text-[#1a73e8]"
                        : "bg-slate-100 text-slate-600"
                    }`}
                  >
                    {step.status}
                  </span>
                </div>
              );
            })}
          </div>

          {/* Expanded Step Detail Drawer */}
          {activeStep && (
            <div className="mt-4 p-4 rounded-xl bg-slate-50 border border-slate-200 text-xs text-slate-700 animate-fadeIn">
              <div className="font-semibold text-slate-900 mb-1">
                {activeStep.title} — Detail View
              </div>
              <p className="text-slate-600 mb-2 leading-relaxed">
                {activeStep.description}
              </p>
              {activeStep.documents.length > 0 && (
                <div className="space-y-1 mt-2">
                  <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                    Attached Artifacts:
                  </span>
                  <div className="flex flex-wrap gap-1.5">
                    {activeStep.documents.map((doc) => (
                      <span
                        key={doc}
                        className="px-2 py-0.5 rounded bg-white border border-slate-200 text-[11px] text-slate-700"
                      >
                        📄 {doc}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Progress Bar */}
          <div className="mt-7">
            <div className="flex justify-between items-center text-xs font-semibold text-slate-400 mb-2">
              <span>CASE COMPLETION</span>
              <span className="text-slate-700">{data.progressPercentage}%</span>
            </div>
            <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
              <div
                className="bg-[#0b7c74] h-2 rounded-full transition-all duration-500 ease-out"
                style={{ width: `${data.progressPercentage}%` }}
              />
            </div>
          </div>

          {/* Card Action Controls */}
          <div className="mt-6 pt-5 border-t border-slate-100 flex items-center justify-between text-xs text-slate-400">
            <button
              type="button"
              onClick={handleReset}
              disabled={isLoading}
              className="text-[#0b7c74] hover:text-[#085b56] hover:underline font-medium"
            >
              ↺ Reset to default state
            </button>
            <span>Click any step to inspect documents</span>
          </div>

          {statusMessage && (
            <div className="mt-3 text-[11px] font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-3 py-1.5 rounded-lg text-center">
              {statusMessage}
            </div>
          )}
        </div>

        <div className="mt-8 text-center sm:text-left">
          <p className="text-xs font-semibold tracking-wider text-[#688a91] uppercase">
            {data.category}
          </p>
        </div>
      </main>

      {/* Bottom Footer */}
      <footer className="w-full max-w-5xl mx-auto pt-6 border-t border-[#163840] flex flex-col sm:flex-row items-center justify-between text-xs text-[#6e8e95] gap-3">
        <span>Team Decipher Group 28 · OnboardFlow Portal</span>
      </footer>
    </div>
  );
}
