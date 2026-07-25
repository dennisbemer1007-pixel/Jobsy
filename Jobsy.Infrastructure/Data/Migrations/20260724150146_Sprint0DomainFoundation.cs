using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint0DomainFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vacancies",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<int>(
                name: "ExtensionCount",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsHighlighted",
                table: "Vacancies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxApplications",
                table: "Vacancies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SalaryTableId",
                table: "Vacancies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Vacancies",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyAdapter",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OpenForWork",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferencesJson",
                table: "Users",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "TokenTransactions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "TokenTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchCompanyId",
                table: "TokenTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NewBalance",
                table: "TokenTransactions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TokenTransactions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OldBalance",
                table: "TokenTransactions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VacancyId",
                table: "TokenTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KvkEstablishmentId",
                table: "Companies",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCompanyId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CandidateAddress",
                table: "Applications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateCity",
                table: "Applications",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateUserId",
                table: "Applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Applications",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferencesSummary",
                table: "Applications",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "Applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Applications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CompanySalaryTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySalaryTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySalaryTables_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EarlyAdapterRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MonthlyGrantTokens = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarlyAdapterRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regions_Companies_OrganizationCompanyId",
                        column: x => x.OrganizationCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackSize = table.Column<int>(type: "integer", nullable: false),
                    PriceEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPricings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenSpendCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    CostTokens = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenSpendCosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VacancyClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyClicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VacancyClicks_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacancyLikes_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VacancyShares_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySalaryRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalaryTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgeYears = table.Column<int>(type: "integer", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySalaryRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySalaryRates_CompanySalaryTables_SalaryTableId",
                        column: x => x.SalaryTableId,
                        principalTable: "CompanySalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionCompanies",
                columns: table => new
                {
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionCompanies", x => new { x.RegionId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_RegionCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegionCompanies_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_SalaryTableId",
                table: "Vacancies",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_ActorUserId",
                table: "TokenTransactions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_BranchCompanyId",
                table: "TokenTransactions",
                column: "BranchCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_CreatedAt",
                table: "TokenTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_VacancyId",
                table: "TokenTransactions",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_KvkEstablishmentId",
                table: "Companies",
                column: "KvkEstablishmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ParentCompanyId",
                table: "Companies",
                column: "ParentCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateUserId",
                table: "Applications",
                column: "CandidateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CreatedAt",
                table: "Applications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryRates_SalaryTableId",
                table: "CompanySalaryRates",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySalaryTables_CompanyId",
                table: "CompanySalaryTables",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformLogs_Category",
                table: "PlatformLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformLogs_CreatedAt",
                table: "PlatformLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegionCompanies_CompanyId",
                table: "RegionCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_OrganizationCompanyId",
                table: "Regions",
                column: "OrganizationCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPricings_PackSize",
                table: "TokenPricings",
                column: "PackSize",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenSpendCosts_Reason",
                table: "TokenSpendCosts",
                column: "Reason",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyClicks_CreatedAt",
                table: "VacancyClicks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyClicks_UserId",
                table: "VacancyClicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyClicks_VacancyId",
                table: "VacancyClicks",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyLikes_CreatedAt",
                table: "VacancyLikes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyLikes_UserId",
                table: "VacancyLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyLikes_VacancyId_UserId",
                table: "VacancyLikes",
                columns: new[] { "VacancyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyShares_CreatedAt",
                table: "VacancyShares",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyShares_UserId",
                table: "VacancyShares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyShares_VacancyId",
                table: "VacancyShares",
                column: "VacancyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_CandidateUserId",
                table: "Applications",
                column: "CandidateUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Companies_ParentCompanyId",
                table: "Companies",
                column: "ParentCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenTransactions_Companies_BranchCompanyId",
                table: "TokenTransactions",
                column: "BranchCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenTransactions_Users_ActorUserId",
                table: "TokenTransactions",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenTransactions_Vacancies_VacancyId",
                table: "TokenTransactions",
                column: "VacancyId",
                principalTable: "Vacancies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_CompanySalaryTables_SalaryTableId",
                table: "Vacancies",
                column: "SalaryTableId",
                principalTable: "CompanySalaryTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Users_CandidateUserId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Companies_ParentCompanyId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_TokenTransactions_Companies_BranchCompanyId",
                table: "TokenTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TokenTransactions_Users_ActorUserId",
                table: "TokenTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TokenTransactions_Vacancies_VacancyId",
                table: "TokenTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_CompanySalaryTables_SalaryTableId",
                table: "Vacancies");

            migrationBuilder.DropTable(
                name: "CompanySalaryRates");

            migrationBuilder.DropTable(
                name: "EarlyAdapterRules");

            migrationBuilder.DropTable(
                name: "PlatformLogs");

            migrationBuilder.DropTable(
                name: "RegionCompanies");

            migrationBuilder.DropTable(
                name: "TokenPricings");

            migrationBuilder.DropTable(
                name: "TokenSpendCosts");

            migrationBuilder.DropTable(
                name: "VacancyClicks");

            migrationBuilder.DropTable(
                name: "VacancyLikes");

            migrationBuilder.DropTable(
                name: "VacancyShares");

            migrationBuilder.DropTable(
                name: "CompanySalaryTables");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_SalaryTableId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_ActorUserId",
                table: "TokenTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_BranchCompanyId",
                table: "TokenTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_CreatedAt",
                table: "TokenTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_VacancyId",
                table: "TokenTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Companies_KvkEstablishmentId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ParentCompanyId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CandidateUserId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CreatedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExtensionCount",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IsHighlighted",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "MaxApplications",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "SalaryTableId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEarlyAdapter",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OpenForWork",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferencesJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "BranchCompanyId",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "NewBalance",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "OldBalance",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "VacancyId",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "KvkEstablishmentId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ParentCompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CandidateAddress",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CandidateCity",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CandidateUserId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PreferencesSummary",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vacancies",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20000)",
                oldMaxLength: 20000);

            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "TokenTransactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
