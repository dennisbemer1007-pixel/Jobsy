using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations;

[DbContext(typeof(JobsyDbContext))]
[Migration("20260729210000_HashVerificationOtpsAndWidenColumns")]
public partial class HashVerificationOtpsAndWidenColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Invalidate any plaintext OTPs still at rest; new codes are stored as SHA-256 hex.
        migrationBuilder.Sql(
            """
            UPDATE "Applications"
            SET "EmailVerificationCode" = NULL,
                "EmailVerificationExpiresAt" = NULL,
                "EmailVerificationFailedAttempts" = 0
            WHERE "EmailVerificationCode" IS NOT NULL
              AND length("EmailVerificationCode") <= 10;
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Users"
            SET "UnsubscribeVerificationCode" = NULL,
                "UnsubscribeVerificationExpiresAt" = NULL,
                "UnsubscribeVerificationFailedAttempts" = 0
            WHERE "UnsubscribeVerificationCode" IS NOT NULL
              AND length("UnsubscribeVerificationCode") <= 10;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "EmailVerificationCode",
            table: "Applications",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(6)",
            oldMaxLength: 6,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "UnsubscribeVerificationCode",
            table: "Users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(6)",
            oldMaxLength: 6,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Applications"
            SET "EmailVerificationCode" = NULL,
                "EmailVerificationExpiresAt" = NULL,
                "EmailVerificationFailedAttempts" = 0
            WHERE "EmailVerificationCode" IS NOT NULL;
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Users"
            SET "UnsubscribeVerificationCode" = NULL,
                "UnsubscribeVerificationExpiresAt" = NULL,
                "UnsubscribeVerificationFailedAttempts" = 0
            WHERE "UnsubscribeVerificationCode" IS NOT NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "EmailVerificationCode",
            table: "Applications",
            type: "character varying(6)",
            maxLength: 6,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "UnsubscribeVerificationCode",
            table: "Users",
            type: "character varying(6)",
            maxLength: 6,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);
    }
}
