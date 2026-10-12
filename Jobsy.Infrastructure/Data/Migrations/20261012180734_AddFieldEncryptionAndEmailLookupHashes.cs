using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldEncryptionAndEmailLookupHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_LocalAuthCredentials_Email",
                table: "LocalAuthCredentials");

            migrationBuilder.AlterColumn<string>(
                name: "DateOfBirth",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailLookupHash",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailLookupHash",
                table: "LocalAuthCredentials",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SnapshotDateOfBirth",
                table: "Applications",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateEmailLookupHash",
                table: "Applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailLookupHash",
                table: "Users",
                column: "EmailLookupHash",
                unique: true,
                filter: "\"EmailLookupHash\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_Email",
                table: "LocalAuthCredentials",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_EmailLookupHash",
                table: "LocalAuthCredentials",
                column: "EmailLookupHash",
                unique: true,
                filter: "\"EmailLookupHash\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateEmailLookupHash",
                table: "Applications",
                column: "CandidateEmailLookupHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailLookupHash",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_LocalAuthCredentials_Email",
                table: "LocalAuthCredentials");

            migrationBuilder.DropIndex(
                name: "IX_LocalAuthCredentials_EmailLookupHash",
                table: "LocalAuthCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CandidateEmailLookupHash",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EmailLookupHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailLookupHash",
                table: "LocalAuthCredentials");

            migrationBuilder.DropColumn(
                name: "CandidateEmailLookupHash",
                table: "Applications");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Users",
                type: "date",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SnapshotDateOfBirth",
                table: "Applications",
                type: "date",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_Email",
                table: "LocalAuthCredentials",
                column: "Email",
                unique: true);
        }
    }
}
