export type UserRole = 'Administrator' | 'OnboardingManager' | 'Reviewer' | 'Client';

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  company?: string;
  clientOrganisationId?: string | null;
  clientOrganisationName?: string | null;
  isActive: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
}

export interface ClientOrganisation {
  id: string;
  name: string;
  category: string;
}

export interface AuthSession {
  token: string;
  user: UserProfile;
}
