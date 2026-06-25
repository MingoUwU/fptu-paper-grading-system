using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fptu.Pgs.AiGrading.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAiCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CredentialSource",
                schema: "grading",
                table: "AIGradingResults",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserAiCredentials",
                schema: "grading",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MaskedApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AllowSystemFallback = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastValidatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAiCredentials_TeacherId_Provider",
                schema: "grading",
                table: "UserAiCredentials",
                columns: new[] { "TeacherId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAiCredentials",
                schema: "grading");

            migrationBuilder.DropColumn(
                name: "CredentialSource",
                schema: "grading",
                table: "AIGradingResults");
        }
    }
}
