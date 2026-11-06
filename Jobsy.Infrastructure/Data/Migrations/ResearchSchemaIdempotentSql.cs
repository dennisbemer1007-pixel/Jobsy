namespace Jobsy.Infrastructure.Data.Migrations;

/// <summary>
/// Idempotent research schema for acceptatie when <c>20261013073839</c> runs after later migrations
/// (e.g. QuickScan) or research objects already exist from a partial deploy.
/// </summary>
internal static class ResearchSchemaIdempotentSql
{
    internal const string Up = """
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchAgeBand" character varying(32);
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchConsentAt" timestamp with time zone;
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchConsentVersion" character varying(32);
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchEducationLevel" character varying(32);
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchGender" character varying(32);
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchNativeLanguage" character varying(16);
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ResearchTestLanguage" character varying(16);
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "ResearchConsentEnabled" boolean NOT NULL DEFAULT false;

        CREATE TABLE IF NOT EXISTS "AssessmentResearchAttempts" (
            "Id" uuid NOT NULL,
            "UserId" uuid,
            "PilotSessionId" uuid,
            "RespondentHash" character varying(64) NOT NULL,
            "TestKey" character varying(64) NOT NULL,
            "TestVersion" integer NOT NULL,
            "TestLanguage" character varying(16) NOT NULL,
            "AgeBand" character varying(32),
            "Gender" character varying(32),
            "EducationLevel" character varying(32),
            "NativeLanguage" character varying(16),
            "Status" integer NOT NULL,
            "CompletedAtUtc" timestamp with time zone NOT NULL,
            "TotalDurationMs" integer,
            "PilotEndComment" character varying(2000),
            "IsPilot" boolean NOT NULL,
            CONSTRAINT "PK_AssessmentResearchAttempts" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "ResearchPilotCampaigns" (
            "Id" uuid NOT NULL,
            "Code" character varying(16) NOT NULL,
            "Title" character varying(200) NOT NULL,
            "IsActive" boolean NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "CreatedByUserId" uuid,
            CONSTRAINT "PK_ResearchPilotCampaigns" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "AssessmentResearchItemResponses" (
            "Id" uuid NOT NULL,
            "AttemptId" uuid NOT NULL,
            "ItemId" integer NOT NULL,
            "ResponseRaw" integer NOT NULL,
            "DurationMs" integer,
            "ItemUnclear" boolean NOT NULL,
            CONSTRAINT "PK_AssessmentResearchItemResponses" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "ResearchPilotSessions" (
            "Id" uuid NOT NULL,
            "CampaignId" uuid NOT NULL,
            "RespondentHash" character varying(64) NOT NULL,
            "AgeBand" character varying(32),
            "Gender" character varying(32),
            "EducationLevel" character varying(32),
            "NativeLanguage" character varying(16),
            "TestLanguage" character varying(16) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_ResearchPilotSessions" PRIMARY KEY ("Id")
        );

        DO $jobsy_research_fk$
        BEGIN
            BEGIN
                ALTER TABLE "AssessmentResearchItemResponses"
                    ADD CONSTRAINT "FK_AssessmentResearchItemResponses_AssessmentResearchAttempts_~"
                    FOREIGN KEY ("AttemptId") REFERENCES "AssessmentResearchAttempts" ("Id") ON DELETE CASCADE;
            EXCEPTION WHEN duplicate_object THEN NULL;
            END;
            BEGIN
                ALTER TABLE "ResearchPilotSessions"
                    ADD CONSTRAINT "FK_ResearchPilotSessions_ResearchPilotCampaigns_CampaignId"
                    FOREIGN KEY ("CampaignId") REFERENCES "ResearchPilotCampaigns" ("Id") ON DELETE CASCADE;
            EXCEPTION WHEN duplicate_object THEN NULL;
            END;
        END $jobsy_research_fk$;

        CREATE INDEX IF NOT EXISTS "IX_AssessmentResearchAttempts_RespondentHash"
            ON "AssessmentResearchAttempts" ("RespondentHash");
        CREATE INDEX IF NOT EXISTS "IX_AssessmentResearchAttempts_UserId_TestKey_CompletedAtUtc"
            ON "AssessmentResearchAttempts" ("UserId", "TestKey", "CompletedAtUtc");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssessmentResearchItemResponses_AttemptId_ItemId"
            ON "AssessmentResearchItemResponses" ("AttemptId", "ItemId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ResearchPilotCampaigns_Code"
            ON "ResearchPilotCampaigns" ("Code");
        CREATE INDEX IF NOT EXISTS "IX_ResearchPilotSessions_CampaignId"
            ON "ResearchPilotSessions" ("CampaignId");
        """;
}
