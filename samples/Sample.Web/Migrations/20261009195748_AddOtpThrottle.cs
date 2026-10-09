using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpThrottle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtpThrottles",
                columns: table => new
                {
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    FailedWindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GenerationCount = table.Column<int>(type: "int", nullable: false),
                    GenerationWindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpThrottles", x => x.PhoneNumber);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpThrottles");
        }
    }
}
