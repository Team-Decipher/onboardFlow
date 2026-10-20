export type UserRole = 'Admin' | 'OnboardingManager' | 'Reviewer' | 'Client';

export type CaseStatus = 'Draft' | 'InProgress' | 'UnderReview' | 'Approved' | 'Rejected' | 'Completed';

export type ClientType = 'Enterprise' | 'SME' | 'Partner';

export type DocumentStatus = 'Pending' | 'Uploaded' | 'Approved' | 'Rejected';

export type ReviewStage = 'Legal' | 'Finance' | 'Executive' | 'Final';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}

export interface DocumentVersion {
  id: string;
  versionNumber: number;
  fileName: string;
  blobUri: string;
  fileSizeBytes: number;
  uploadedAt: string;
  rejectionReason?: string;
}

export interface ChecklistItem {
  id: string;
  title: string;
  description: string;
  isMandatory: boolean;
  status: DocumentStatus;
  currentStage: ReviewStage;
  dueDate?: string;
  documentId?: string;
  fileName?: string;
  fileSizeBytes?: number;
  rejectionReason?: string;
  versions?: DocumentVersion[];
}

export interface OnboardingCase {
  id: string;
  caseNumber: string;
  clientName: string;
  clientType: ClientType;
  status: CaseStatus;
  ownerId: string;
  ownerName: string;
  createdAt: string;
  updatedAt: string;
  progressPercentage: number;
  checklistItems: ChecklistItem[];
}
