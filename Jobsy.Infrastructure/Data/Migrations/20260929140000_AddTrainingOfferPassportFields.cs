using System;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Passport course curation fields on TrainingOffer. Existing rows stay ShowInPassport=false.
    /// </summary>
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260929140000_AddTrainingOfferPassportFields")]
    public partial class AddTrainingOfferPassportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateCode",
                table: "TrainingOffers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Delivery",
                table: "TrainingOffers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationUnit",
                table: "TrainingOffers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationValue",
                table: "TrainingOffers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "TrainingOffers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPartner",
                table: "TrainingOffers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "TrainingOffers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInPassport",
                table: "TrainingOffers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "TrainingOffers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingOffers_ShowInPassport",
                table: "TrainingOffers",
                column: "ShowInPassport");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingOffers_ShowInPassport",
                table: "TrainingOffers");

            migrationBuilder.DropColumn(name: "AffiliateCode", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "Delivery", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "DurationUnit", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "DurationValue", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "IsFree", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "IsPartner", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "Location", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "ShowInPassport", table: "TrainingOffers");
            migrationBuilder.DropColumn(name: "Type", table: "TrainingOffers");
        }
    }
}
