using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MartinBot.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktestRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BacktestRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Pair = table.Column<string>(type: "TEXT", nullable: false),
                    Timeframe = table.Column<string>(type: "TEXT", nullable: false),
                    From = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    To = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    InitialCash = table.Column<decimal>(type: "TEXT", nullable: false),
                    FeeBps = table.Column<decimal>(type: "TEXT", nullable: false),
                    SlippageBps = table.Column<decimal>(type: "TEXT", nullable: false),
                    StrategyName = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FinalEquity = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalReturn = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxDrawdown = table.Column<decimal>(type: "TEXT", nullable: true),
                    Sharpe = table.Column<decimal>(type: "TEXT", nullable: true),
                    TradeCount = table.Column<int>(type: "INTEGER", nullable: true),
                    WinRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestRuns_CreatedAt",
                table: "BacktestRuns",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacktestRuns");
        }
    }
}
