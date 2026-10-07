using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioContentType",
                table: "Meeting",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFileName",
                table: "Meeting",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AudioSizeBytes",
                table: "Meeting",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AudioUploadedAt",
                table: "Meeting",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioUploadedByUserId",
                table: "Meeting",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioContentType",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "AudioFileName",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "AudioSizeBytes",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "AudioUploadedAt",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "AudioUploadedByUserId",
                table: "Meeting");
        }
    }
}
