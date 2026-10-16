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
}
