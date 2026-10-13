using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOccupationDayInLife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OccupationDayInLives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscoId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Uri = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TitleNl = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Morning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Midday = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Afternoon = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HighlightsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    VariesNote = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    SourceModel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ThinSource = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupationDayInLives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OccupationDayInLives_EscoId",
                table: "OccupationDayInLives",
                column: "EscoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OccupationDayInLives");
        }
    }
}
