"use client";

import type { FormEvent } from "react";
import type {
  OrganisationDetailsErrors,
  OrganisationDetailsField,
  OrganisationDetailsValues,
} from "../types";

interface OrganisationDetailsFormProps {
  values: OrganisationDetailsValues;
  errors: OrganisationDetailsErrors;
  isSaving: boolean;
  onChange: (field: OrganisationDetailsField, value: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
}

const inputClassName =
  "w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-[#0d7377] focus:outline-none focus:ring-2 focus:ring-[#0d7377]/20";

export function OrganisationDetailsForm({
  values,
  errors,
  isSaving,
  onChange,
  onSubmit,
  onCancel,
}: OrganisationDetailsFormProps) {
  const fieldError = (field: OrganisationDetailsField) => errors[field];

  const renderError = (field: OrganisationDetailsField) =>
    fieldError(field) ? (
      <p id={`${field}-error`} className="mt-1 text-xs text-red-600">
        {fieldError(field)}
      </p>
    ) : null;

  const renderFieldClass = (field: OrganisationDetailsField) =>
    `${inputClassName} ${fieldError(field) ? "border-red-500" : ""}`;

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div>
          <label htmlFor="organisation-name" className="mb-1 block text-xs font-medium text-slate-700">
            Organisation name *
          </label>
          <input
            id="organisation-name"
            name="name"
            autoComplete="organization"
            required
            value={values.name}
            onChange={(event) => onChange("name", event.target.value)}
            aria-invalid={Boolean(fieldError("name"))}
            aria-describedby={fieldError("name") ? "name-error" : undefined}
            className={renderFieldClass("name")}
          />
          {renderError("name")}
        </div>

        <div>
          <label htmlFor="organisation-client-type" className="mb-1 block text-xs font-medium text-slate-700">
            Client type *
          </label>
          <select
            id="organisation-client-type"
            name="clientType"
            required
            value={values.clientType}
            onChange={(event) => onChange("clientType", event.target.value)}
            aria-invalid={Boolean(fieldError("clientType"))}
            aria-describedby={fieldError("clientType") ? "clientType-error" : undefined}
            className={renderFieldClass("clientType")}
          >
            <option value="Enterprise">Enterprise</option>
            <option value="SME">SME</option>
            <option value="Partner">Partner</option>
          </select>
          {renderError("clientType")}
        </div>

        <div>
          <label htmlFor="organisation-contact-name" className="mb-1 block text-xs font-medium text-slate-700">
            Contact name *
          </label>
          <input
            id="organisation-contact-name"
            name="primaryContact"
            autoComplete="name"
            required
            value={values.primaryContact}
            onChange={(event) => onChange("primaryContact", event.target.value)}
            aria-invalid={Boolean(fieldError("primaryContact"))}
            aria-describedby={fieldError("primaryContact") ? "primaryContact-error" : undefined}
            className={renderFieldClass("primaryContact")}
          />
          {renderError("primaryContact")}
        </div>

        <div>
          <label htmlFor="organisation-contact-email" className="mb-1 block text-xs font-medium text-slate-700">
            Contact email *
          </label>
          <input
            id="organisation-contact-email"
            name="contactEmail"
            type="email"
            autoComplete="email"
            required
            value={values.contactEmail}
            onChange={(event) => onChange("contactEmail", event.target.value)}
            aria-invalid={Boolean(fieldError("contactEmail"))}
            aria-describedby={fieldError("contactEmail") ? "contactEmail-error" : undefined}
            className={renderFieldClass("contactEmail")}
          />
          {renderError("contactEmail")}
        </div>

        <div>
          <label htmlFor="organisation-phone" className="mb-1 block text-xs font-medium text-slate-700">
            Phone number
          </label>
          <input
            id="organisation-phone"
            name="phoneNumber"
            type="tel"
            autoComplete="tel"
            value={values.phoneNumber}
            onChange={(event) => onChange("phoneNumber", event.target.value)}
            className={inputClassName}
          />
        </div>

        <div>
          <label htmlFor="organisation-registration" className="mb-1 block text-xs font-medium text-slate-700">
            Business registration number
          </label>
          <input
            id="organisation-registration"
            name="businessRegistrationNumber"
            value={values.businessRegistrationNumber}
            onChange={(event) =>
              onChange("businessRegistrationNumber", event.target.value)
            }
            className={inputClassName}
          />
        </div>

        <div className="md:col-span-2">
          <label htmlFor="organisation-address" className="mb-1 block text-xs font-medium text-slate-700">
            Business address
          </label>
          <input
            id="organisation-address"
            name="businessAddress"
            autoComplete="street-address"
            value={values.businessAddress}
            onChange={(event) => onChange("businessAddress", event.target.value)}
            className={inputClassName}
          />
        </div>
      </div>

      <div className="flex items-center gap-2 pt-1">
        <button
          type="button"
          onClick={onCancel}
          disabled={isSaving}
          className="rounded-md border border-slate-300 bg-slate-100 px-4 py-2 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-200 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSaving}
          className="rounded-md bg-[#0d7377] px-4 py-2 text-xs font-semibold text-white transition-colors hover:bg-[#095e62] disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSaving ? "Saving..." : "Save changes"}
        </button>
      </div>
    </form>
  );
}
