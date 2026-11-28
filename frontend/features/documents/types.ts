export interface UploadDocumentResponse {
  documentId: string;
  versionNumber: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface CaseDocumentListItem {
  documentId: string;
  checklistItemId: string;
  currentVersionNumber: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
  uploadedBy: string;
}

export class DocumentUploadError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = "DocumentUploadError";
    this.status = status;
    this.code = code;
  }
}
