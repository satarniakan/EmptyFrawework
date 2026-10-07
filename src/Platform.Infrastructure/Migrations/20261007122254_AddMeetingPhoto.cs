using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "Meeting",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Meeting",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PhotoSizeBytes",
                table: "Meeting",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhotoUploadedAt",
                table: "Meeting",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUploadedByUserId",
                table: "Meeting",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "PhotoSizeBytes",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "PhotoUploadedAt",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "PhotoUploadedByUserId",
                table: "Meeting");
        }
    }
}
