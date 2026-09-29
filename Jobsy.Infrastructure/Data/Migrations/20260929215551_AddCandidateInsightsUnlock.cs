using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateInsightsUnlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CandidateInsightsEnabled",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CandidateInsightsUnlockDays",
                table: "PlatformFeatureSettings",
                type: "integer",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<bool>(
                name: "CandidateInsightsUnlockPerBranch",
                table: "PlatformFeatureSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CandidateInsightsUnlockRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateInsightsUnlockRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlockRequests_Companies_BranchCompanyId",
                        column: x => x.BranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlockRequests_Companies_WalletCompanyId",
                        column: x => x.WalletCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlockRequests_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlockRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CandidateInsightsUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<int>(type: "integer", nullable: false),
                    ScopeCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PriceTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateInsightsUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlocks_Companies_ScopeCompanyId",
                        column: x => x.ScopeCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlocks_Companies_WalletCompanyId",
                        column: x => x.WalletCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlocks_TokenTransactions_TokenTransaction~",
                        column: x => x.TokenTransactionId,
                        principalTable: "TokenTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateInsightsUnlocks_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlockRequests_BranchCompanyId_Status",
                table: "CandidateInsightsUnlockRequests",
                columns: new[] { "BranchCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlockRequests_HandledByUserId",
                table: "CandidateInsightsUnlockRequests",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlockRequests_RequestedByUserId",
                table: "CandidateInsightsUnlockRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlockRequests_WalletCompanyId_Status",
                table: "CandidateInsightsUnlockRequests",
                columns: new[] { "WalletCompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlocks_ActorUserId",
                table: "CandidateInsightsUnlocks",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlocks_IdempotencyKey",
                table: "CandidateInsightsUnlocks",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlocks_ScopeCompanyId_ExpiresAtUtc",
                table: "CandidateInsightsUnlocks",
                columns: new[] { "ScopeCompanyId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlocks_TokenTransactionId",
                table: "CandidateInsightsUnlocks",
                column: "TokenTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInsightsUnlocks_WalletCompanyId_ScopeKind_ScopeCom~",
                table: "CandidateInsightsUnlocks",
                columns: new[] { "WalletCompanyId", "ScopeKind", "ScopeCompanyId" });

            // Seed InsightsUnlock spend cost (12 tokens) when missing.
            migrationBuilder.Sql(
                """
                INSERT INTO "TokenSpendCosts" ("Id", "Reason", "CostTokens", "IsActive")
                SELECT gen_random_uuid(), 6, 12, TRUE
                WHERE NOT EXISTS (
                    SELECT 1 FROM "TokenSpendCosts" WHERE "Reason" = 6
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateInsightsUnlockRequests");

            migrationBuilder.DropTable(
                name: "CandidateInsightsUnlocks");

            migrationBuilder.DropColumn(
                name: "CandidateInsightsEnabled",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "CandidateInsightsUnlockDays",
                table: "PlatformFeatureSettings");

            migrationBuilder.DropColumn(
                name: "CandidateInsightsUnlockPerBranch",
                table: "PlatformFeatureSettings");
        }
    }
}
