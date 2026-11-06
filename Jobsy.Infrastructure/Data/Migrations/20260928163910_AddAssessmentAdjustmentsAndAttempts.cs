using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAdjustmentsAndAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateAssessmentAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Variant = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAssessmentAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAdjustments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateAssessmentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Variant = table.Column<int>(type: "integer", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScoresJson = table.Column<string>(type: "text", nullable: true),
                    ReportJson = table.Column<string>(type: "text", nullable: true),
                    ReportVersion = table.Column<int>(type: "integer", nullable: true),
                    PreviousSnapshotJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAssessmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAdjustments_AttemptId",
                table: "CandidateAssessmentAdjustments",
                column: "AttemptId",
                unique: true,
                filter: "\"AttemptId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAdjustments_UserId_Kind_Variant",
                table: "CandidateAssessmentAdjustments",
                columns: new[] { "UserId", "Kind", "Variant" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAdjustments_UserId_Kind_Variant_Idempote~",
                table: "CandidateAssessmentAdjustments",
                columns: new[] { "UserId", "Kind", "Variant", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAttempts_UserId_Kind_Variant_Status",
                table: "CandidateAssessmentAttempts",
                columns: new[] { "UserId", "Kind", "Variant", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateAssessmentAdjustments");

            migrationBuilder.DropTable(
                name: "CandidateAssessmentAttempts");
        }
    }
}
