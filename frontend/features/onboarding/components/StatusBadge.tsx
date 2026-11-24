import React from "react";

export function StatusBadge({ status }: { status: string }) {
  const norm = status?.trim().toLowerCase() || "";

  if (norm === "in review" || norm === "inreview") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#eef2ff] text-[#4f46e5]">
        In review
      </span>
    );
  }

  if (norm === "overdue") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#fef3c7] text-[#b45309]">
        Overdue
      </span>
    );
  }

  if (norm === "needs correction" || norm === "needscorrection") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#ffe4e6] text-[#e11d48]">
        Needs correction
      </span>
    );
  }

  if (norm === "approved") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#dcfce7] text-[#15803d]">
        Approved
      </span>
    );
  }

  if (norm === "completed") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#dcfce7] text-[#15803d]">
        Completed
      </span>
    );
  }

  if (norm === "missing") {
    return (
      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-[#f1f5f9] text-[#64748b]">
        Missing
      </span>
    );
  }

  return (
    <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-slate-100 text-slate-700">
      {status}
    </span>
  );
}
