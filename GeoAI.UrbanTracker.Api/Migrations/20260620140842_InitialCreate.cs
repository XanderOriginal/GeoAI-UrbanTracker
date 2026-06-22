using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoAI.UrbanTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    NdviBefore = table.Column<double>(type: "double precision", nullable: false),
                    NdviAfter = table.Column<double>(type: "double precision", nullable: false),
                    NdviChangePercent = table.Column<double>(type: "double precision", nullable: false),
                    BuiltUpAreaChangePercent = table.Column<double>(type: "double precision", nullable: false),
                    GreenAreaChangePercent = table.Column<double>(type: "double precision", nullable: false),
                    GeminiSummary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_AnalysisRequests_AnalysisRequestId",
                        column: x => x.AnalysisRequestId,
                        principalTable: "AnalysisRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SatelliteImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBeforeImage = table.Column<bool>(type: "boolean", nullable: false),
                    CaptureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    CloudCoveragePercent = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatelliteImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SatelliteImages_AnalysisRequests_AnalysisRequestId",
                        column: x => x.AnalysisRequestId,
                        principalTable: "AnalysisRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_AnalysisRequestId",
                table: "AnalysisResults",
                column: "AnalysisRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SatelliteImages_AnalysisRequestId",
                table: "SatelliteImages",
                column: "AnalysisRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropTable(
                name: "SatelliteImages");

            migrationBuilder.DropTable(
                name: "AnalysisRequests");
        }
    }
}
