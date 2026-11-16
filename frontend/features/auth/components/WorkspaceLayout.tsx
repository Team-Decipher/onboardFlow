"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "../hooks/useAuth";

interface NavItem {
  label: string;
  href: string;
  badge?: string;
}

export function WorkspaceLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { user, initials, logout } = useAuth();

  // Dynamic Navigation menu items based on the active role
  const getNavItems = (): NavItem[] => {
    switch (user?.role) {
      case "Administrator":
        return [
          { label: "Overview", href: "/dashboard" },
          { label: "Client organisations", href: "/admin/organisations" },
          { label: "Client types", href: "/admin/client-types" },
          { label: "Checklist templates", href: "/admin/checklists" },
          { label: "Approval workflows", href: "/admin/workflows" },
          { label: "User accounts", href: "/admin/users" },
          { label: "Reviewer workload", href: "/admin/reviewer-workload" },
          { label: "Audit log", href: "/admin/audit-log" },
          { label: "System settings", href: "/admin/settings" },
        ];
      case "OnboardingManager":
        return [
          { label: "Overview", href: "/dashboard" },
          { label: "Onboarding cases", href: "/manager/cases" },
          { label: "Overdue items", href: "/manager/overdue" },
          { label: "Reports & insights", href: "/manager/reports" },
        ];
      case "Reviewer":
        return [
          { label: "Overview", href: "/dashboard" },
          { label: "My review queue", href: "/reviewer/queue" },
          { label: "Notifications", href: "/reviewer/notifications" },
          { label: "My profile", href: "/reviewer/profile" },
        ];
      case "Client":
        return [
          { label: "Your onboarding", href: "/dashboard" },
          { label: "Your documents", href: "/client/documents" },
          { label: "Notifications", href: "/client/notifications" },
          { label: "Profile & preferences", href: "/client/profile" },
        ];
      default:
        return [{ label: "Overview", href: "/dashboard" }];
    }
  };

  const navItems = getNavItems();

  return (
    <div className="min-h-screen bg-[#f3f6f7] flex font-sans text-slate-800">
      {/* Dark Sidebar Matching Report Diagrams */}
      <aside className="w-64 bg-[#0d272d] text-white flex flex-col justify-between p-5 border-r border-[#143d46] shrink-0">
        <div>
          {/* Workspace Title */}
          <div className="mb-6">
            <Link href="/dashboard" className="block">
              <span className="text-xl font-bold tracking-tight text-white">
                OnboardFlow
              </span>
            </Link>
            <span className="text-[10px] uppercase font-bold tracking-widest text-[#5e8b94] mt-0.5 block">
              {user?.role === "Client" ? "Client Portal" : "Workspace"}
            </span>
          </div>

          {/* Nav List */}
          <nav className="space-y-1">
            {navItems.map((item) => {
              const isActive = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-medium transition-all ${
                    isActive
                      ? "bg-[#0b7c74] text-white shadow-sm font-semibold"
                      : "text-[#9bb7be] hover:bg-[#133740] hover:text-white"
                  }`}
                >
                  <span>{item.label}</span>
                  {item.badge && (
                    <span className="text-[10px] bg-[#1a4a55] px-2 py-0.5 rounded-full font-semibold">
                      {item.badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* Sidebar Footer */}
        <div className="pt-4 border-t border-[#173e46] text-xs text-[#6e8e95]">
          <div className="font-semibold text-slate-200">{user?.fullName || "Guest User"}</div>
          <div className="text-[11px] text-[#5e8b94] capitalize">{user?.role || "Not signed in"}</div>
          {user?.clientOrganisationName && (
            <div className="text-[10px] text-[#4d767e] truncate mt-0.5">{user.clientOrganisationName}</div>
          )}
        </div>
      </aside>

      {/* Main Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top Navbar */}
        <header className="h-16 bg-white border-b border-slate-200 px-6 flex items-center justify-between shadow-xs">
          <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
            <span>Workspace</span>
            <span>/</span>
            <span className="text-slate-800 capitalize font-semibold">
              {pathname.split("/").filter(Boolean).pop() || "Overview"}
            </span>
          </div>

          {/* Top-Right Navigation Controls with Dynamic Initials */}
          <div className="flex items-center gap-4">
            <span className="text-xs text-slate-600 hover:text-slate-900 cursor-pointer hidden sm:inline">
              Help centre
            </span>
            <div className="flex items-center gap-1.5 text-xs text-slate-600 cursor-pointer">
              <span>Notifications</span>
              <span className="bg-slate-200 text-slate-700 font-semibold px-1.5 py-0.5 rounded-full text-[10px]">
                3
              </span>
            </div>

            {/* Dynamic Initials Badge */}
            <div className="flex items-center gap-2 pl-2 border-l border-slate-200">
              <div
                className="w-8 h-8 rounded-full bg-[#0b7c74] text-white flex items-center justify-center font-bold text-xs tracking-wider shadow-sm select-none"
                title={`${user?.fullName} (${user?.role})`}
              >
                {initials}
              </div>
              <button
                type="button"
                onClick={logout}
                className="text-xs text-slate-500 hover:text-red-600 transition-colors ml-1"
                title="Sign out"
              >
                Sign out
              </button>
            </div>
          </div>
        </header>

        {/* Content Body */}
        <main className="flex-1 p-6 sm:p-8 overflow-y-auto">{children}</main>
      </div>
    </div>
  );
}

