using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MartinBot.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterSweepRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParameterSweepRuns",
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
                    ParameterGridJson = table.Column<string>(type: "TEXT", nullable: true),
                    OptimizationMetric = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCombinations = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedCombinations = table.Column<int>(type: "INTEGER", nullable: true),
                    BestParametersJson = table.Column<string>(type: "TEXT", nullable: true),
                    BestMetricValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    BestTotalReturn = table.Column<decimal>(type: "TEXT", nullable: true),
                    BestMaxDrawdown = table.Column<decimal>(type: "TEXT", nullable: true),
                    BestSharpe = table.Column<decimal>(type: "TEXT", nullable: true),
                    BestTradeCount = table.Column<int>(type: "INTEGER", nullable: true),
                    BestWinRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    BestDroppedIntents = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterSweepRuns", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ParameterSweepRuns");
        }
    }
}
