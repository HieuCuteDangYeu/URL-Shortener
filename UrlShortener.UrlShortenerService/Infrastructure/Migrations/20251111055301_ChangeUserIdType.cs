using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.UrlShortenerService.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""ShortLinks""
                ALTER COLUMN ""UserId"" TYPE uuid
                USING CAST(NULL AS uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""ShortLinks""
                ALTER COLUMN ""UserId"" TYPE integer
                USING CAST(NULL AS integer);
            ");
        }
    }
}
