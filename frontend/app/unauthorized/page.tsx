"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";

export default function UnauthorizedPage() {
  const router = useRouter();

  return (
    <div className="min-h-screen bg-[#0d272d] text-white flex flex-col justify-center items-center px-6">
      <div className="bg-white text-slate-900 rounded-[22px] p-8 max-w-md w-full shadow-2xl border border-slate-100 text-center">
        <div className="w-12 h-12 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg
            className="w-6 h-6"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
        </div>

        <h2 className="text-lg font-bold text-slate-800">
          You don&apos;t have access to this page
        </h2>
        <p className="text-xs text-slate-500 mt-2 leading-relaxed">
          Return to your workspace or contact your administrator.
        </p>

        <div className="mt-6 flex flex-col gap-2">
          <Link
            href="/dashboard"
            className="w-full py-2.5 px-4 bg-[#0b7c74] hover:bg-[#096963] text-white font-semibold text-xs rounded-xl transition-colors shadow-sm"
          >
            Return to your workspace
          </Link>
          <button
            type="button"
            onClick={() => {
              localStorage.removeItem("onboardflow_token");
              localStorage.removeItem("onboardflow_user");
              document.cookie = "onboardflow_token=; path=/; max-age=0";
              document.cookie = "onboardflow_role=; path=/; max-age=0";
              router.push("/login");
            }}
            className="text-xs text-slate-500 hover:text-slate-800 transition-colors py-1"
          >
            Sign in with another account
          </button>
        </div>
      </div>
    </div>
  );
}

