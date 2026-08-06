using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260806210000_PartnerAffiliateProfiles")]
    public partial class PartnerAffiliateProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferredByPartnerUserId",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerTrackingCode",
                table: "CompanyRegistrations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartnerCommissionRate",
                table: "SalesCommercialSettings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.05m);

            migrationBuilder.CreateTable(
                name: "PartnerAffiliateProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KvkNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAffiliateProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerAffiliateProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ReferredByPartnerUserId",
                table: "Companies",
                column: "ReferredByPartnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAffiliateProfiles_TrackingCode",
                table: "PartnerAffiliateProfiles",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAffiliateProfiles_UserId",
                table: "PartnerAffiliateProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_ReferredByPartnerUserId",
                table: "Companies",
                column: "ReferredByPartnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_ReferredByPartnerUserId",
                table: "Companies");

            migrationBuilder.DropTable(name: "PartnerAffiliateProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ReferredByPartnerUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReferredByPartnerUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PartnerTrackingCode",
                table: "CompanyRegistrations");

            migrationBuilder.DropColumn(
                name: "PartnerCommissionRate",
                table: "SalesCommercialSettings");
        }
    }
}
