using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateDiplomaEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SnapshotDiplomaEvaluationsJson",
                table: "Applications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateDiplomaEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiplomaTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuingBody = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IssuingBodyOther = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EquivalentLevelText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EquivalentLevelCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DocumentFileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    DocumentContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DocumentContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    DocumentSizeBytes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateDiplomaEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateDiplomaEvaluations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateDiplomaEvaluations_UserId_CreatedAtUtc",
                table: "CandidateDiplomaEvaluations",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateDiplomaEvaluations");

            migrationBuilder.DropColumn(
                name: "SnapshotDiplomaEvaluationsJson",
                table: "Applications");
        }
    }
}
