using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "Meeting",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Meeting");
        }
    }
}
