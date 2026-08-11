using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingFlyerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingFlyerSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subheadline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Intro = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    BulletPoints = table.Column<string>(type: "text", nullable: false),
                    PromoFreeText = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    PromoDiscountText = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    CtaTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CtaBody = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QrCaption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QrPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FooterNote = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingFlyerSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingFlyerSettings");
        }
    }
}
