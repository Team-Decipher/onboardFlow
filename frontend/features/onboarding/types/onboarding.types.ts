export interface OnboardingCaseSummary {
  id: string;
  referenceCode: string;
  clientOrganisationId: string;
  clientOrganisationName: string;
  clientTypeId: string;
  clientTypeName: string;
  ownerUserId: string;
  ownerName: string;
  ownerShortName: string;
  targetCompletionDate: string;
  dueDateDisplay: string;
  status: string;
  approvedCount: number;
  totalRequiredCount: number;
  progressPercentage: number;
  progressDisplay: string;
  createdAtUtc: string;
}

export interface CaseCounts {
  all: number;
  inProgress: number;
  completed: number;
}

export interface OnboardingCaseListResponse {
  items: OnboardingCaseSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  counts: CaseCounts;
}

export interface OnboardingCaseItem {
  id: string;
  documentName: string;
  isRequired: boolean;
  orderIndex: number;
  status: string; // "Approved" | "In review" | "Needs correction" | "Missing"
  assignedReviewerId?: string | null;
  reviewerName: string;
  dueDate?: string | null;
  dueDateDisplay: string;
  uploadedFileName?: string | null;
}

export interface CaseTimelineEvent {
  title: string;
  description: string;
  status: "Completed" | "Current" | "Pending";
  dateDisplay: string;
}

export interface CaseActivity {
  id: string;
  action: string;
  actor: string;
  details: string;
  timestampUtc: string;
  dateDisplay: string;
}

export interface ClientOrganisationBrief {
  id: string;
  name: string;
  category: string;
  contactName?: string | null;
  contactEmail?: string | null;
  phoneNumber?: string | null;
}

export interface ClientTypeBrief {
  id: string;
  name: string;
  defaultChecklistTemplateId?: string | null;
  defaultChecklistTemplateName?: string | null;
  defaultChecklistTemplateVersion?: string | null;
  requiredCount: number;
  optionalCount: number;
  previewItems: string[];
}

export interface CaseOwnerBrief {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface CaseFormOptions {
  organisations: ClientOrganisationBrief[];
  clientTypes: ClientTypeBrief[];
  owners: CaseOwnerBrief[];
}

export interface OnboardingCaseDetail {
  id: string;
  referenceCode: string;
  clientOrganisation: ClientOrganisationBrief;
  clientType: ClientTypeBrief;
  owner: CaseOwnerBrief;
  targetCompletionDate: string;
  dueDateDisplay: string;
  status: string;
  approvedCount: number;
  totalRequiredCount: number;
  totalCount: number;
  progressPercentage: number;
  progressDisplay: string;
  attentionBannerMessage?: string | null;
  attentionBannerSubtitle?: string | null;
  items: OnboardingCaseItem[];
  timeline: CaseTimelineEvent[];
  activities: CaseActivity[];
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}
