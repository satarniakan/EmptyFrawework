using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Meeting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MeetingLink = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MinutesHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinutesSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meeting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingInvitee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Response = table.Column<int>(type: "int", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attendance = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingInvitee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingInvitee_Meeting_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meeting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingTimeProposal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    ProposedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProposedStartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTimeProposal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingTimeProposal_Meeting_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meeting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_CreatedByUserId",
                table: "Meeting",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_StartAt",
                table: "Meeting",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingInvitee_MeetingId_UserId",
                table: "MeetingInvitee",
                columns: new[] { "MeetingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingInvitee_UserId",
                table: "MeetingInvitee",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTimeProposal_MeetingId_Status",
                table: "MeetingTimeProposal",
                columns: new[] { "MeetingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTask_AssignedUserId",
                table: "WorkTask",
                column: "AssignedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingInvitee");

            migrationBuilder.DropTable(
                name: "MeetingTimeProposal");

            migrationBuilder.DropTable(
                name: "WorkTask");

            migrationBuilder.DropTable(
                name: "Meeting");
        }
    }
}
