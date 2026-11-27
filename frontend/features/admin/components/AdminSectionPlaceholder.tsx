import { WorkspaceLayout } from "@/features/auth/components/WorkspaceLayout";

interface AdminSectionPlaceholderProps {
  title: string;
}

export function AdminSectionPlaceholder({ title }: AdminSectionPlaceholderProps) {
  return (
    <WorkspaceLayout breadcrumb={title}>
      <h1 className="text-2xl font-bold text-slate-900">{title}</h1>
    </WorkspaceLayout>
  );
}
