using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.AnalyticsService.Migrations
{
    /// <inheritdoc />
    public partial class InitAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Browser = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Referer = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clicks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clicks_CreatedAt",
                table: "Clicks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Clicks_ShortCode",
                table: "Clicks",
                column: "ShortCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clicks");
        }
    }
}
