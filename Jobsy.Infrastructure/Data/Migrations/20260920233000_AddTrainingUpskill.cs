using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jobsy.Infrastructure.Data.Migrations
{
    [DbContext(typeof(JobsyDbContext))]
    [Migration("20260920233000_AddTrainingUpskill")]
    public class AddTrainingUpskill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Network = table.Column<int>(type: "integer", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FieldsCsv = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CplEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CpaEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IntakeFeeEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    StartFeeEuro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldsCsv = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeysCsv = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ExternalPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingOffers_TrainingProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "TrainingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CandidateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmailHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Campaign = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClickedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OutboundUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingClicks_TrainingOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "TrainingOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingClicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrainingConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClickId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingConversions_TrainingClicks_ClickId",
                        column: x => x.ClickId,
                        principalTable: "TrainingClicks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingProviders_Kind",
                table: "TrainingProviders",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingProviders_IsActive",
                table: "TrainingProviders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingOffers_ProviderId",
                table: "TrainingOffers",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClicks_OfferId",
                table: "TrainingClicks",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClicks_CandidateHash",
                table: "TrainingClicks",
                column: "CandidateHash");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClicks_EmailHash",
                table: "TrainingClicks",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClicks_UserId",
                table: "TrainingClicks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingConversions_ClickId_Kind",
                table: "TrainingConversions",
                columns: new[] { "ClickId", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TrainingConversions");
            migrationBuilder.DropTable(name: "TrainingClicks");
            migrationBuilder.DropTable(name: "TrainingOffers");
            migrationBuilder.DropTable(name: "TrainingProviders");
        }
    }
}
