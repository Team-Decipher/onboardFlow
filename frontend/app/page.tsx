"use client";

import { useSyncExternalStore } from "react";
import LoginPage from "./login/page";
import DashboardPage from "./dashboard/page";

const emptySubscribe = () => () => {};

export default function Home() {
  const isMounted = useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false
  );

  const token = useSyncExternalStore(
    (callback) => {
      window.addEventListener("storage", callback);
      return () => window.removeEventListener("storage", callback);
    },
    () => localStorage.getItem("onboardflow_token"),
    () => null
  );

  if (!isMounted) {
    return (
      <div className="min-h-screen bg-[#0d272d] flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-emerald-400"></div>
      </div>
    );
  }

  return token ? <DashboardPage /> : <LoginPage />;
}
