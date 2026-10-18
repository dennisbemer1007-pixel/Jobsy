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
        migrationBuilder.AddColumn<string>(
            name: "AdminFlagOverridesJson",
            table: "PlatformFeatureSettings",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<bool>(
            name: "TestJourneyIllustrationEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "OnboardingFinaleV2Enabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "OccupationDayInLifeEnabled",
            table: "PlatformFeatureSettings",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AdminFlagOverridesJson", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "TestJourneyIllustrationEnabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "OnboardingFinaleV2Enabled", table: "PlatformFeatureSettings");
        migrationBuilder.DropColumn(name: "OccupationDayInLifeEnabled", table: "PlatformFeatureSettings");
    }
}
