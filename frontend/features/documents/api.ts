import {
  CaseDocumentListItem,
  DocumentUploadError,
  UploadDocumentResponse,
} from "./types";

const getApiUrl = () =>
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

const getToken = () =>
  typeof window !== "undefined"
    ? localStorage.getItem("onboardflow_token")
    : null;

export function mapDocumentErrorMessage(
  code?: string,
  serverDetail?: string,
  status?: number
): string {
  switch (code) {
    case "file_empty":
      return "The selected file is empty.";
    case "file_missing":
      return "Please select a file to upload.";
    case "request_too_large":
      return "This file is too large to upload.";
    case "document_exists":
      return "A document was already uploaded for this item.";
    case "file_rejected":
      return serverDetail || "The selected file was rejected by the server.";
    default:
      if (status === 413) {
        return "This file is too large to upload.";
      }
      if (status === 401 || status === 403) {
        return "You do not have permission to upload to this case.";
      }
      if (status === 404) {
        return "Case or checklist item was not found.";
      }
      return serverDetail || "Upload failed. Please try again.";
  }
}

export function uploadDocumentWithProgress(
  caseId: string,
  itemId: string,
  file: File,
  onProgress?: (percent: number) => void
): Promise<UploadDocumentResponse> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const url = `${getApiUrl()}/api/cases/${encodeURIComponent(
      caseId
    )}/checklist-items/${encodeURIComponent(itemId)}/documents`;

    xhr.open("POST", url, true);

    const token = getToken();
    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable && onProgress) {
        const pct = Math.round((event.loaded / event.total) * 100);
        onProgress(pct);
      }
    };

    xhr.onload = () => {
      let body: Record<string, unknown> | null = null;
      try {
        body = xhr.responseText ? JSON.parse(xhr.responseText) : null;
      } catch {
        body = null;
      }

      if (xhr.status >= 200 && xhr.status < 300 && body) {
        resolve(body as unknown as UploadDocumentResponse);
        return;
      }

      const code = typeof body?.code === "string" ? body.code : undefined;
      const detail =
        typeof body?.detail === "string" ? body.detail : undefined;
      const message = mapDocumentErrorMessage(code, detail, xhr.status);
      reject(new DocumentUploadError(message, xhr.status, code));
    };

    xhr.onerror = () => {
      reject(
        new DocumentUploadError(
          "Network error while uploading. Please check your connection and retry.",
          0
        )
      );
    };

    const formData = new FormData();
    formData.append("file", file);
    xhr.send(formData);
  });
}

export async function listCaseDocuments(
  caseId: string
): Promise<CaseDocumentListItem[]> {
  const token = getToken();
  const res = await fetch(
    `${getApiUrl()}/api/cases/${encodeURIComponent(caseId)}/documents`,
    {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    }
  );

  if (!res.ok) {
    throw new Error(`Failed to load documents (HTTP ${res.status}).`);
  }

  return (await res.json()) as CaseDocumentListItem[];
}
