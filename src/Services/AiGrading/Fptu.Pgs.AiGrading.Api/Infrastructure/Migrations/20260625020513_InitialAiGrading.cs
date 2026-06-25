using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fptu.Pgs.AiGrading.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAiGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "grading");

            migrationBuilder.CreateTable(
                name: "AIGradingResults",
                schema: "grading",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    OverallFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewScoreSynchronized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGradingResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AICriterionGrades",
                schema: "grading",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiGradingResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AwardedScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingPointsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AICriterionGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AICriterionGrades_AIGradingResults_AiGradingResultId",
                        column: x => x.AiGradingResultId,
                        principalSchema: "grading",
                        principalTable: "AIGradingResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AICriterionGrades_AiGradingResultId_CriterionId",
                schema: "grading",
                table: "AICriterionGrades",
                columns: new[] { "AiGradingResultId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIGradingResults_SubmissionId_GradedAtUtc",
                schema: "grading",
                table: "AIGradingResults",
                columns: new[] { "SubmissionId", "GradedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AICriterionGrades",
                schema: "grading");

            migrationBuilder.DropTable(
                name: "AIGradingResults",
                schema: "grading");
        }
    }
}
