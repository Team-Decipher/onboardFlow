import type { ClientOrganisation } from "./types";

export const ORGANISATIONS_STORAGE_KEY = "onboardflow_organisation_overrides";

export function readOrganisationOverrides(): ClientOrganisation[] {
  try {
    const stored = localStorage.getItem(ORGANISATIONS_STORAGE_KEY);
    if (!stored) return [];

    const parsed: unknown = JSON.parse(stored);
    if (!Array.isArray(parsed)) return [];
    return parsed as ClientOrganisation[];
  } catch {
    return [];
  }
}

export function saveOrganisationOverride(
  organisation: ClientOrganisation
): void {
  const overrides = readOrganisationOverrides();
  const existingIndex = overrides.findIndex(
    (item) => item.id === organisation.id || item.clientCode === organisation.clientCode
  );

  if (existingIndex >= 0) {
    overrides[existingIndex] = organisation;
  } else {
    overrides.push(organisation);
  }

  localStorage.setItem(ORGANISATIONS_STORAGE_KEY, JSON.stringify(overrides));
}

export function withOrganisationOverrides(
  organisations: ClientOrganisation[]
): ClientOrganisation[] {
  const overrides = readOrganisationOverrides();
  return organisations.map(
    (organisation) =>
      overrides.find(
        (override) =>
          override.id === organisation.id ||
          override.clientCode === organisation.clientCode
      ) ?? organisation
  );
}
