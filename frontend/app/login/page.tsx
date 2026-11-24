"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";

interface RoleOption {
  label: string;
  roleKey: string;
  placeholder: string;
  redirect: string;
}

const ROLES: RoleOption[] = [
  {
    label: "Administrator",
    roleKey: "Administrator",
    placeholder: "admin@example.com",
    redirect: "/client-organisations",
  },
  {
    label: "Onboarding Manager",
    roleKey: "OnboardingManager",
    placeholder: "kasun@example.com",
    redirect: "/overview",
  },
  {
    label: "Reviewer",
    roleKey: "Reviewer",
    placeholder: "amara@example.com",
    redirect: "/review-queue",
  },
  {
    label: "Client",
    roleKey: "Client",
    placeholder: "nimali@example.com",
    redirect: "/your-onboarding",
  },
];

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

export default function LoginPage() {
  const router = useRouter();
  const [selectedRoleIndex, setSelectedRoleIndex] = useState(0);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [emailTouched, setEmailTouched] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [loginError, setLoginError] = useState(false);

  const selectedRole = ROLES[selectedRoleIndex];
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  const emailInvalid = emailTouched && email !== "" && !isValidEmail(email);

  const handleRoleSelect = (index: number) => {
    setSelectedRoleIndex(index);
    setLoginError(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setLoginError(false);

    try {
      const res = await fetch(`${apiUrl}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email,
          password,
          role: selectedRole.roleKey,
        }),
      });

      if (res.ok) {
        const data = await res.json();
        localStorage.setItem("onboardflow_token", data.token);
        localStorage.setItem("onboardflow_user", JSON.stringify(data.user));
        document.cookie = `onboardflow_token=${data.token}; path=/; max-age=28800; SameSite=Lax`;
        document.cookie = `onboardflow_role=${data.user.role}; path=/; max-age=28800; SameSite=Lax`;

        const redirect =
          ROLES.find((r) => r.roleKey === data.user?.role)?.redirect ??
          "/client-organisations";
        router.push(redirect);
      } else {
        setLoginError(true);
      }
    } catch {
      setLoginError(true);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex font-sans">
      {/* ── Left Panel ── */}
      <div className="hidden lg:flex lg:w-1/2 bg-[#0F292E] flex-col justify-between p-10">
        {/* Logo */}
        <div>
          <span className="text-white font-bold text-lg">OnboardFlow</span>
        </div>

        {/* Centre copy */}
        <div>
          <h1 className="text-white text-3xl font-bold leading-snug">
            Great partnerships start here.
          </h1>
          <p className="text-gray-300 text-sm mt-3">
            One place for your clients, documents and every step in between.
          </p>

          {/* Preview card */}
          <div className="bg-white rounded-2xl p-5 shadow-md mt-8">
            <p className="text-slate-800 font-semibold text-sm mb-4">
              A smoother start
            </p>

            <div className="space-y-3">
              {/* Row 1 */}
              <div className="flex items-center justify-between">
                <span className="text-slate-700 text-sm">Create your case</span>
                <span className="bg-emerald-100 text-emerald-700 text-xs px-2.5 py-0.5 rounded-full font-medium">
                  Approved
                </span>
              </div>
              {/* Row 2 */}
              <div className="flex items-center justify-between">
                <span className="text-slate-700 text-sm">Collect documents</span>
                <span className="bg-emerald-100 text-emerald-700 text-xs px-2.5 py-0.5 rounded-full font-medium">
                  Approved
                </span>
              </div>
              {/* Row 3 */}
              <div className="flex items-center justify-between">
                <span className="text-slate-700 text-sm">Review &amp; approve</span>
                <span className="bg-blue-100 text-blue-700 text-xs px-2.5 py-0.5 rounded-full font-medium">
                  In review
                </span>
              </div>
            </div>

            {/* Progress bar */}
            <div className="mt-5 bg-gray-100 rounded-full h-1.5 overflow-hidden">
              <div className="bg-teal-600 h-1.5 rounded-full w-[65%]" />
            </div>
          </div>
        </div>

        {/* Footer */}
        <p className="text-[10px] text-gray-400 uppercase tracking-widest">
          CLIENT ONBOARDING &amp; DOCUMENT WORKFLOW
        </p>
      </div>

      {/* ── Right Panel ── */}
      <div className="w-full lg:w-1/2 bg-white flex items-center justify-center">
        <div className="w-full max-w-md px-10 py-12 mx-auto">
          <h2 className="text-2xl font-bold text-slate-900">
            Sign in as {selectedRole.label}
          </h2>
          <p className="text-sm text-slate-500 mt-1">
            Choose your role, then enter your account details.
          </p>

          {/* Role tabs */}
          <div className="mt-6 grid grid-cols-2 gap-2">
            {ROLES.map((role, i) => (
              <button
                key={role.roleKey}
                type="button"
                onClick={() => handleRoleSelect(i)}
                className={
                  i === selectedRoleIndex
                    ? "bg-[#09191C] text-white rounded-lg py-2.5 px-3 text-sm font-semibold text-left"
                    : "bg-white text-gray-700 border border-gray-200 rounded-lg py-2.5 px-3 text-sm text-left"
                }
              >
                {role.label}
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit}>
            {/* Email */}
            <div className="mt-5">
              <label
                htmlFor="email"
                className="block text-xs font-medium text-slate-600 mb-1"
              >
                Email address
              </label>
              <input
                id="email"
                type="text"
                required
                value={email}
                placeholder={selectedRole.placeholder}
                onChange={(e) => {
                  setEmail(e.target.value);
                  setLoginError(false);
                }}
                onBlur={() => setEmailTouched(true)}
                className={`w-full px-3.5 py-2.5 rounded-lg border text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-600 focus:border-transparent transition-all ${
                  emailInvalid
                    ? "border-red-400 bg-red-50"
                    : "border-slate-200 bg-slate-50"
                }`}
              />
              {emailInvalid && (
                <p className="text-red-500 text-xs mt-1">
                  Enter a valid email address.
                </p>
              )}
            </div>

            {/* Password */}
            <div className="mt-4">
              <div className="flex items-center justify-between mb-1">
                <label
                  htmlFor="password"
                  className="text-xs font-medium text-slate-600"
                >
                  Password
                </label>
                <Link
                  href="/forgot-password"
                  className="text-teal-600 text-xs hover:underline"
                >
                  Forgot password?
                </Link>
              </div>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  required
                  value={password}
                  placeholder="••••••••"
                  onChange={(e) => {
                    setPassword(e.target.value);
                    setLoginError(false);
                  }}
                  className="w-full px-3.5 py-2.5 rounded-lg border border-slate-200 bg-slate-50 text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-600 focus:border-transparent transition-all pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute inset-y-0 right-3 flex items-center text-slate-400 hover:text-slate-700"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? (
                    /* Eye-off */
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      className="h-4 w-4"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      strokeWidth={2}
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-5 0-9-4-9-7a9.77 9.77 0 012.197-5.19M9.88 9.88A3 3 0 0112 9a3 3 0 013 3m0 0a3 3 0 01-.879 2.121M3 3l18 18"
                      />
                    </svg>
                  ) : (
                    /* Eye */
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      className="h-4 w-4"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      strokeWidth={2}
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      />
                    </svg>
                  )}
                </button>
              </div>
            </div>

            {/* API error banner */}
            {loginError && (
              <div className="bg-[#FDF2F2] border border-red-200 rounded-lg p-3 mt-4">
                <p className="text-red-700 text-sm font-semibold">
                  Unable to sign in
                </p>
                <p className="text-red-600 text-xs mt-0.5">
                  Check your email and password, then try again.
                </p>
              </div>
            )}

            {/* Submit */}
            <button
              type="submit"
              disabled={isLoading}
              className="mt-6 w-full bg-[#0D7A78] text-white py-3 rounded-lg font-semibold text-sm flex items-center justify-center gap-2 disabled:opacity-70 hover:bg-[#0b6c6a] transition-colors"
            >
              {isLoading ? (
                <>
                  <svg
                    className="animate-spin h-4 w-4 text-white"
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8v8H4z"
                    />
                  </svg>
                  Signing in...
                </>
              ) : (
                `Sign in as ${selectedRole.label}`
              )}
            </button>
          </form>

          {/* Helper text */}
          <p className="text-xs text-slate-400 mt-4 text-center">
            Your administrator provides your account. Need access? Contact your
            organisation.
          </p>
        </div>
      </div>
    </div>
  );
}
