import { apiClient } from './apiClient';
import { OnboardingCase } from '../types';

export const caseService = {
  async getCases(): Promise<OnboardingCase[]> {
    const res = await apiClient.get<OnboardingCase[]>('/cases');
    return res.data;
  },

  async getCaseById(id: string): Promise<OnboardingCase> {
    const res = await apiClient.get<OnboardingCase>(`/cases/${id}`);
    return res.data;
  },

  async createCase(data: Partial<OnboardingCase>): Promise<OnboardingCase> {
    const res = await apiClient.post<OnboardingCase>('/cases', data);
    return res.data;
  },

  async updateCaseStatus(id: string, status: string): Promise<OnboardingCase> {
    const res = await apiClient.put<OnboardingCase>(`/cases/${id}/status`, { status });
    return res.data;
  },
};
