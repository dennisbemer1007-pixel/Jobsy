namespace Jobsy.Infrastructure.Data.Migrations;

/// <summary>
/// Idempotent PlatformFeatureSettings columns for acceptatie when migrations run out of order
/// or the API serves traffic before <see cref="JobsyDbSeeder.MigrateAsync"/> finishes.
/// </summary>
internal static class PlatformFeatureSchemaIdempotentSql
{
    internal const string EnsurePassportPdfDnaBlocksEnabled = """
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "PassportPdfDnaBlocksEnabled" boolean NOT NULL DEFAULT true;
        """;

    internal const string EnsureFeatureFlagAdminOverrides = """
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "AdminFlagOverridesJson" text NOT NULL DEFAULT '[]';
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "ReferenceVerificationEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestJourneyIllustrationEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "OnboardingFinaleV2Enabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "OccupationDayInLifeEnabled" boolean NOT NULL DEFAULT true;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestEditModeEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "SamplePdfEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestDepthValueMockupEnabled" boolean NOT NULL DEFAULT true;
        """;
}
