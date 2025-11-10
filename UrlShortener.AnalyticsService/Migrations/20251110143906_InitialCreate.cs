using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.AnalyticsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClickRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortUrlId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Browser = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Device = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Referrer = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickRecords_ShortUrlId_Timestamp",
                table: "ClickRecords",
                columns: new[] { "ShortUrlId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickRecords_Timestamp",
                table: "ClickRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ClickRecords_UserId",
                table: "ClickRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickRecords");
        }
    }
}
