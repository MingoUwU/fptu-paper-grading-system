using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fptu.Pgs.ReviewScore.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialReviewScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "score");

            migrationBuilder.CreateTable(
                name: "SubmissionScores",
                schema: "score",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiGradingResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AiFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AiConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    AiProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AiGradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeacherScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    TeacherFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TeacherGradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinalScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriterionScores",
                schema: "score",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AiScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AiFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TeacherScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    TeacherFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriterionScores_SubmissionScores_SubmissionScoreId",
                        column: x => x.SubmissionScoreId,
                        principalSchema: "score",
                        principalTable: "SubmissionScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreAuditLogs",
                schema: "score",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    NewScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreAuditLogs_SubmissionScores_SubmissionScoreId",
                        column: x => x.SubmissionScoreId,
                        principalSchema: "score",
                        principalTable: "SubmissionScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriterionScores_SubmissionScoreId_CriterionId",
                schema: "score",
                table: "CriterionScores",
                columns: new[] { "SubmissionScoreId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAuditLogs_SubmissionScoreId_OccurredAtUtc",
                schema: "score",
                table: "ScoreAuditLogs",
                columns: new[] { "SubmissionScoreId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_AiGradingResultId",
                schema: "score",
                table: "SubmissionScores",
                column: "AiGradingResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionScores_SubmissionId",
                schema: "score",
                table: "SubmissionScores",
                column: "SubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriterionScores",
                schema: "score");

            migrationBuilder.DropTable(
                name: "ScoreAuditLogs",
                schema: "score");

            migrationBuilder.DropTable(
                name: "SubmissionScores",
                schema: "score");
        }
    }
}
