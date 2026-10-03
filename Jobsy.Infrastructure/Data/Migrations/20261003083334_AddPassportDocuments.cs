using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmedFieldsJson",
                table: "CandidateUploadedCvs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PassportDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrimaryLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SecondaryLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IncludesPage2 = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesContact = table.Column<bool>(type: "boolean", nullable: false),
                    PassportPartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareLinkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportDocuments_PassportPartners_PassportPartnerId",
                        column: x => x.PassportPartnerId,
                        principalTable: "PassportPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PassportDocuments_Users_CandidateUserId",
                        column: x => x.CandidateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassportTextTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    TargetLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportTextTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassportTextTranslations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PassportDocuments_CandidateUserId",
                table: "PassportDocuments",
                column: "CandidateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportDocuments_PassportPartnerId",
                table: "PassportDocuments",
                column: "PassportPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PassportDocuments_PublicId",
                table: "PassportDocuments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassportTextTranslations_UserId_FieldKey_TargetLanguage",
                table: "PassportTextTranslations",
                columns: new[] { "UserId", "FieldKey", "TargetLanguage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassportDocuments");

            migrationBuilder.DropTable(
                name: "PassportTextTranslations");

            migrationBuilder.DropColumn(
                name: "ConfirmedFieldsJson",
                table: "CandidateUploadedCvs");
        }
    }
}
