using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20261018160000_FeatureFlagAdminOverrides")]
public partial class FeatureFlagAdminOverrides : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "AdminFlagOverridesJson" text NOT NULL DEFAULT '[]';
            ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "TestJourneyIllustrationEnabled" boolean NOT NULL DEFAULT false;
            ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "OnboardingFinaleV2Enabled" boolean NOT NULL DEFAULT false;
            ALTER TABLE "PlatformFeatureSettings" ADD COLUMN IF NOT EXISTS "OccupationDayInLifeEnabled" boolean NOT NULL DEFAULT true;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AdminFlagOverridesJson", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "TestJourneyIllustrationEnabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "OnboardingFinaleV2Enabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "OccupationDayInLifeEnabled", table: "PlatformFeatureSettings");
    }
}
