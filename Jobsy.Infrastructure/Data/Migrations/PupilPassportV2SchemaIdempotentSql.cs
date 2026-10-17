namespace Jobsy.Infrastructure.Data.Migrations;

/// <summary>Safe column adds when API boots before EF migrate finishes (acceptatie hotfix pattern).</summary>
internal static class PupilPassportV2SchemaIdempotentSql
{
    internal const string EnsureColumns = """
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "PupilPassportV2Enabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "StudentPassportEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoFavoriteSubjectsJson" text;
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoProfileKey" character varying(64);
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoSideJobAvailabilityJson" text;
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoTravelModeKey" character varying(32);
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoOwnWords" character varying(2000);
        ALTER TABLE "PupilProgresses" ADD COLUMN IF NOT EXISTS "VoLearningGoalsJson" text;
        """;
}
