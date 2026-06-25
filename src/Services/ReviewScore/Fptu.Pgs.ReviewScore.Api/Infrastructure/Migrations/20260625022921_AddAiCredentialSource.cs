using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fptu.Pgs.ReviewScore.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCredentialSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiCredentialSource",
                schema: "score",
                table: "SubmissionScores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCredentialSource",
                schema: "score",
                table: "SubmissionScores");
        }
    }
}
