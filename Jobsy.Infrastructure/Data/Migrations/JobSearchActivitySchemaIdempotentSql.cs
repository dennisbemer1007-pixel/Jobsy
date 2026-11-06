namespace Jobsy.Infrastructure.Data.Migrations;

/// <summary>
/// Idempotent DDL when EF history is behind but acc API must serve activity log (R8b).
/// Mirrors <c>20261026102721_AddJobSearchActivity</c> and attachments migration.
/// </summary>
internal static class JobSearchActivitySchemaIdempotentSql
{
    internal const string EnsureSchema = """
        ALTER TABLE "PlatformFeatureSettings"
            ADD COLUMN IF NOT EXISTS "ActivityLogEnabled" boolean NOT NULL DEFAULT FALSE;

        CREATE TABLE IF NOT EXISTS "ActivityAgreements" (
            "CandidateUserId" uuid NOT NULL,
            "ActivitiesPerPeriod" integer NOT NULL,
            "PeriodStart" date NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_ActivityAgreements" PRIMARY KEY ("CandidateUserId"),
            CONSTRAINT "FK_ActivityAgreements_Users_CandidateUserId" FOREIGN KEY ("CandidateUserId")
                REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS "JobSearchActivities" (
            "Id" uuid NOT NULL,
            "CandidateUserId" uuid NOT NULL,
            "ActivityDate" date NOT NULL,
            "Kind" integer NOT NULL,
            "Employer" character varying(200) NOT NULL,
            "FunctionTitle" character varying(200) NOT NULL,
            "ContactMethod" character varying(200),
            "Result" character varying(2000),
            "Source" integer NOT NULL,
            "SourceEntityType" character varying(64),
            "SourceEntityId" uuid,
            "AttachmentId" uuid,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_JobSearchActivities" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_JobSearchActivities_Users_CandidateUserId" FOREIGN KEY ("CandidateUserId")
                REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS "IX_JobSearchActivities_CandidateUserId_ActivityDate"
            ON "JobSearchActivities" ("CandidateUserId", "ActivityDate");

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_JobSearchActivities_CandidateUserId_Source_SourceEntityType~"
            ON "JobSearchActivities" ("CandidateUserId", "Source", "SourceEntityType", "SourceEntityId")
            WHERE "Source" = 0 AND "SourceEntityType" IS NOT NULL AND "SourceEntityId" IS NOT NULL;

        CREATE TABLE IF NOT EXISTS "JobSearchActivityAttachments" (
            "Id" uuid NOT NULL,
            "CandidateUserId" uuid NOT NULL,
            "FileName" character varying(200) NOT NULL,
            "ContentType" character varying(100) NOT NULL,
            "SizeBytes" integer NOT NULL,
            "Content" bytea NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_JobSearchActivityAttachments" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_JobSearchActivityAttachments_Users_CandidateUserId" FOREIGN KEY ("CandidateUserId")
                REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS "IX_JobSearchActivityAttachments_CandidateUserId"
            ON "JobSearchActivityAttachments" ("CandidateUserId");
        """;
}
