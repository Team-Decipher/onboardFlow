import { apiClient } from './apiClient';

export interface ReviewDocumentPayload {
  approved: boolean;
  comments?: string;
  nextStage?: string;
}

export const documentService = {
  async uploadDocument(checklistItemId: string, file: File): Promise<{ documentId: string; blobUri: string }> {
    const formData = new FormData();
    formData.append('checklistItemId', checklistItemId);
    formData.append('file', file);

    const res = await apiClient.post('/documents/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },

  async reviewDocument(documentId: string, payload: ReviewDocumentPayload) {
    const res = await apiClient.post(`/documents/${documentId}/review`, payload);
    return res.data;
  },

  async getVersions(documentId: string) {
    const res = await apiClient.get(`/documents/${documentId}/versions`);
    return res.data;
  },
};
