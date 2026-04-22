using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MartinBot.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktestRunStrategyParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StrategyParametersJson",
                table: "BacktestRuns",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StrategyParametersJson",
                table: "BacktestRuns");
        }
    }
}
