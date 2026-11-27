export interface LinkedCase {
  id: string;
  organisationId: string;
  caseNumber: string;
  title: string;
  status: string;
  progressPercentage: number;
  reviewerName: string;
  targetCompletionDate?: string | null;
  createdAtUtc?: string;
}

export interface ClientOrganisation {
  id: string;
  clientCode: string;
  name: string;
  city: string;
  clientType: string;
  primaryContact: string;
  contactEmail: string;
  phoneNumber?: string;
  businessRegistrationNumber?: string;
  businessAddress?: string;
  activeCasesCount?: number;
  casesSummary: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
  linkedCases?: LinkedCase[];
}

export interface OrganisationDetailsValues {
  name: string;
  clientType: string;
  primaryContact: string;
  contactEmail: string;
  phoneNumber: string;
  businessRegistrationNumber: string;
  businessAddress: string;
}

export type OrganisationDetailsField = keyof OrganisationDetailsValues;
export type OrganisationDetailsErrors = Partial<
  Record<OrganisationDetailsField, string>
>;

export interface OrganisationAccessUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  status: string;
  lastLogin: string;
}
