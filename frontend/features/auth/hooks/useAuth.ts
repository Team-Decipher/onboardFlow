"use client";

import { useSyncExternalStore, useCallback } from "react";
import { useRouter } from "next/navigation";
import { UserProfile } from "../types/auth.types";

export function getInitials(name?: string): string {
  if (!name) return "??";
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) {
    return parts[0].substring(0, 2).toUpperCase();
  }
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function subscribe(callback: () => void) {
  window.addEventListener("storage", callback);
  return () => window.removeEventListener("storage", callback);
}

const emptySubscribe = () => () => {};

export function useAuth() {
  const router = useRouter();

  const isMounted = useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false
  );

  const token = useSyncExternalStore(
    subscribe,
    () => localStorage.getItem("onboardflow_token"),
    () => null
  );

  const userString = useSyncExternalStore(
    subscribe,
    () => localStorage.getItem("onboardflow_user"),
    () => null
  );

  let user: UserProfile | null = null;
  if (userString) {
    try {
      user = JSON.parse(userString);
    } catch {
      user = null;
    }
  }

  const logout = useCallback(() => {
    localStorage.removeItem("onboardflow_token");
    localStorage.removeItem("onboardflow_user");
    document.cookie = "onboardflow_token=; path=/; max-age=0";
    document.cookie = "onboardflow_role=; path=/; max-age=0";
    window.dispatchEvent(new Event("storage"));
    router.push("/login");
  }, [router]);

  return {
    user,
    token,
    isLoading: !isMounted,
    logout,
    initials: getInitials(user?.fullName),
    isAuthenticated: !!token && !!user,
  };
}
