CREATE TABLE IF NOT EXISTS "Organisations" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ClientCode" character varying(50) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "City" character varying(100) DEFAULT '',
    "ClientType" character varying(50) NOT NULL DEFAULT 'Enterprise',
    "PrimaryContact" character varying(200) NOT NULL,
    "ContactEmail" character varying(200) NOT NULL,
    "PhoneNumber" character varying(50) DEFAULT '',
    "BusinessRegistrationNumber" character varying(100) DEFAULT '',
    "BusinessAddress" character varying(500) DEFAULT '',
    "ActiveCasesCount" integer NOT NULL DEFAULT 0,
    "CasesSummary" character varying(100) DEFAULT '1 active',
    "Status" character varying(50) NOT NULL DEFAULT 'Active',
    "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAtUtc" timestamp with time zone
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Organisations_ClientCode" ON "Organisations" ("ClientCode");

CREATE TABLE IF NOT EXISTS "LinkedCases" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "OrganisationId" uuid NOT NULL REFERENCES "Organisations" ("Id") ON DELETE CASCADE,
    "CaseNumber" character varying(50) NOT NULL,
    "Title" character varying(250) NOT NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'Active',
    "ProgressPercentage" integer NOT NULL DEFAULT 0,
    "ReviewerName" character varying(200) DEFAULT '',
    "TargetCompletionDate" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "IX_LinkedCases_OrganisationId" ON "LinkedCases" ("OrganisationId");
