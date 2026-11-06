using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBeheerTodos : Migration
    {
        private const string CotanNipMailBody =
            """
            Aan: directie@nip.nl
            Onderwerp: Vraag over hervatting COTAN-beoordelingen voor nieuwe instrumenten

            Geachte directie,

            Ik ben oprichter van Lobsy, een Nederlands loopbaanplatform voor werkzoekenden. Daarvoor ontwikkelen wij vragenlijsten voor competenties, beroepsinteresses (RIASEC), werkwaarden en cultuur/persoonlijkheid, beschikbaar in vijf talen. Bij de ontwikkeling volgen we het COTAN-beoordelingssysteem. De handleiding en het testmateriaal zijn gereed en de validatiestudie gaat binnenkort van start.

            Op uw website lees ik dat de COTAN voorlopig geen nieuwe instrumenten opneemt in de beoordelingsprocedure. Ik heb daarover twee vragen:

            1. Is er al een verwachting wanneer nieuwe aanmeldingen weer mogelijk zijn?
            2. Kunnen wij ons nu alvast registreren of op een wachtlijst plaatsen, zodat we kunnen indienen zodra dat weer kan?

            Verwijst u ons, zolang de COTAN geen nieuwe aanmeldingen aanneemt, bij voorkeur naar een andere manier om de kwaliteit van onze vragenlijsten onafhankelijk te laten toetsen? Dan horen wij dat ook graag.

            Alvast hartelijk dank voor uw reactie.

            Met vriendelijke groet,

            Dennis
            Lobsy, lobsy.nl
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeheerTodos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeheerTodos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeheerTodos_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BeheerTodos_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeheerTodos_CreatedByUserId",
                table: "BeheerTodos",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BeheerTodos_SeedKey",
                table: "BeheerTodos",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BeheerTodos_UpdatedByUserId",
                table: "BeheerTodos",
                column: "UpdatedByUserId");

            var seedAt = "2026-10-13T16:00:00+00:00";
            var cotanBody = CotanNipMailBody.Trim().Replace("'", "''");
            migrationBuilder.Sql(
                $"""
                 INSERT INTO "BeheerTodos" ("Id", "SeedKey", "Title", "Body", "DueDate", "Priority", "Status", "Category", "CreatedAtUtc", "UpdatedAtUtc", "CreatedByUserId", "UpdatedByUserId")
                 SELECT
                     'b1e0e0e0-e0e0-4000-8000-000000000001',
                     'seed-cotan-nip-mail',
                     'Mail NIP: wanneer gaat COTAN weer open?',
                     '{cotanBody}',
                     NULL,
                     2,
                     0,
                     'COTAN',
                     TIMESTAMPTZ '{seedAt}',
                     TIMESTAMPTZ '{seedAt}',
                     NULL,
                     NULL
                 WHERE NOT EXISTS (SELECT 1 FROM "BeheerTodos" WHERE "SeedKey" = 'seed-cotan-nip-mail');
                 """);

            migrationBuilder.Sql($"""
                INSERT INTO "BeheerTodos" ("Id", "SeedKey", "Title", "Body", "DueDate", "Priority", "Status", "Category", "CreatedAtUtc", "UpdatedAtUtc", "CreatedByUserId", "UpdatedByUserId")
                SELECT
                    'b1e0e0e0-e0e0-4000-8000-000000000002',
                    'seed-render-encryption-keys',
                    'Encryptiesleutels op Render zetten (acc + prod, API + Web) zodat PR #638 kan mergen',
                    '',
                    NULL,
                    1,
                    0,
                    'Infra',
                    TIMESTAMPTZ '{seedAt}',
                    TIMESTAMPTZ '{seedAt}',
                    NULL,
                    NULL
                WHERE NOT EXISTS (SELECT 1 FROM "BeheerTodos" WHERE "SeedKey" = 'seed-render-encryption-keys');
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "BeheerTodos" ("Id", "SeedKey", "Title", "Body", "DueDate", "Priority", "Status", "Category", "CreatedAtUtc", "UpdatedAtUtc", "CreatedByUserId", "UpdatedByUserId")
                SELECT
                    'b1e0e0e0-e0e0-4000-8000-000000000003',
                    'seed-acc-feature-flags',
                    'Flags aanzetten op acceptatie: ProactiveMatchingEnabled, EmployersEnabled, MatchCalibrationV1Enabled, TestsV2Enabled',
                    '',
                    NULL,
                    1,
                    0,
                    'Acceptatie',
                    TIMESTAMPTZ '{seedAt}',
                    TIMESTAMPTZ '{seedAt}',
                    NULL,
                    NULL
                WHERE NOT EXISTS (SELECT 1 FROM "BeheerTodos" WHERE "SeedKey" = 'seed-acc-feature-flags');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeheerTodos");
        }
    }
}
