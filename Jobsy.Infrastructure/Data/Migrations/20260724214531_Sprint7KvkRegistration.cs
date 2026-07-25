using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7KvkRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KvkEstablishmentId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EstablishmentName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EstablishmentAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ActivationToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOrganizationCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBranchCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    StubPassword = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyRegistrations_Companies_CreatedBranchCompanyId",
                        column: x => x.CreatedBranchCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyRegistrations_Companies_CreatedOrganizationCompanyId",
                        column: x => x.CreatedOrganizationCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompanyRegistrations_Users_CreatedUserId",
                        column: x => x.CreatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LocalAuthCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalAuthCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalAuthCredentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstablishmentTakeoverRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstablishmentTakeoverRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstablishmentTakeoverRequests_Companies_TargetCompanyId",
                        column: x => x.TargetCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstablishmentTakeoverRequests_CompanyRegistrations_Registra~",
                        column: x => x.RegistrationId,
                        principalTable: "CompanyRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstablishmentTakeoverRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_ActivationToken",
                table: "CompanyRegistrations",
                column: "ActivationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_ContactEmail",
                table: "CompanyRegistrations",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CreatedAt",
                table: "CompanyRegistrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CreatedBranchCompanyId",
                table: "CompanyRegistrations",
                column: "CreatedBranchCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CreatedOrganizationCompanyId",
                table: "CompanyRegistrations",
                column: "CreatedOrganizationCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRegistrations_CreatedUserId",
                table: "CompanyRegistrations",
                column: "CreatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_CreatedAt",
                table: "EstablishmentTakeoverRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_DecidedByUserId",
                table: "EstablishmentTakeoverRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_RegistrationId",
                table: "EstablishmentTakeoverRequests",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_Status",
                table: "EstablishmentTakeoverRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EstablishmentTakeoverRequests_TargetCompanyId",
                table: "EstablishmentTakeoverRequests",
                column: "TargetCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_Email",
                table: "LocalAuthCredentials",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalAuthCredentials_UserId",
                table: "LocalAuthCredentials",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstablishmentTakeoverRequests");

            migrationBuilder.DropTable(
                name: "LocalAuthCredentials");

            migrationBuilder.DropTable(
                name: "CompanyRegistrations");
        }
    }
}
