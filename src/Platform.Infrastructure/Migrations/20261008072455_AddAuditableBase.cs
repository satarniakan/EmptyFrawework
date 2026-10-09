using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "MeetingTimeProposal",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeetingTimeProposal",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingTimeProposal",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "MeetingTimeProposal",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MeetingInvitee",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "MeetingInvitee",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeetingInvitee",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingInvitee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "MeetingInvitee",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "MeetingDecision",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MeetingDecision",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingDecision",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "MeetingDecision",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Meeting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Meeting",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Meeting",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTimeProposal_IsDeleted",
                table: "MeetingTimeProposal",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingInvitee_IsDeleted",
                table: "MeetingInvitee",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDecision_IsDeleted",
                table: "MeetingDecision",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_IsDeleted",
                table: "Meeting",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingTimeProposal_IsDeleted",
                table: "MeetingTimeProposal");

            migrationBuilder.DropIndex(
                name: "IX_MeetingInvitee_IsDeleted",
                table: "MeetingInvitee");

            migrationBuilder.DropIndex(
                name: "IX_MeetingDecision_IsDeleted",
                table: "MeetingDecision");

            migrationBuilder.DropIndex(
                name: "IX_Meeting_IsDeleted",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MeetingTimeProposal");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeetingTimeProposal");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingTimeProposal");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MeetingTimeProposal");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MeetingInvitee");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MeetingInvitee");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeetingInvitee");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingInvitee");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MeetingInvitee");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MeetingDecision");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MeetingDecision");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingDecision");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MeetingDecision");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Meeting");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Meeting");
        }
    }
}
