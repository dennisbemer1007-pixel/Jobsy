using System;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20260926180000_CandidatePrivacyConsents")]
public partial class CandidatePrivacyConsents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ParentalConsentAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ParentalConsentEmail",
            table: "Users",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ParentalConsentTokenHash",
            table: "Users",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ParentalConsentTokenExpiresAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TalentPoolConsentAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TalentPoolConsentVersion",
            table: "Users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TestAiConsentAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TestAiConsentVersion",
            table: "Users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ParentalConsentAt", table: "Users");
        migrationBuilder.DropColumn(name: "ParentalConsentEmail", table: "Users");
        migrationBuilder.DropColumn(name: "ParentalConsentTokenHash", table: "Users");
        migrationBuilder.DropColumn(name: "ParentalConsentTokenExpiresAt", table: "Users");
        migrationBuilder.DropColumn(name: "TalentPoolConsentAt", table: "Users");
        migrationBuilder.DropColumn(name: "TalentPoolConsentVersion", table: "Users");
        migrationBuilder.DropColumn(name: "TestAiConsentAt", table: "Users");
        migrationBuilder.DropColumn(name: "TestAiConsentVersion", table: "Users");
    }
}
