using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fptu.Pgs.ReviewScore.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradingAssignments",
                schema: "score",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TeacherGradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradingAssignments_ExamId_TeacherId",
                schema: "score",
                table: "GradingAssignments",
                columns: new[] { "ExamId", "TeacherId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingAssignments_SubmissionId",
                schema: "score",
                table: "GradingAssignments",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingAssignments_TeacherId_Status",
                schema: "score",
                table: "GradingAssignments",
                columns: new[] { "TeacherId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradingAssignments",
                schema: "score");
        }
    }
}
