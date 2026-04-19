using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MartinBot.Domain.Migrations
{
    /// <inheritdoc />
    public partial class DropBacktestRunsCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BacktestRuns_CreatedAt",
                table: "BacktestRuns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_CreatedAt",
                table: "BacktestRuns",
                column: "CreatedAt");
        }
    }
}
