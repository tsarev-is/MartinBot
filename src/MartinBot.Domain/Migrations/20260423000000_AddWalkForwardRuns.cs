using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MartinBot.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkForwardRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalkForwardRuns",
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
                    TrainDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TestDays = table.Column<int>(type: "INTEGER", nullable: false),
                    StepDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWindows = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedWindows = table.Column<int>(type: "INTEGER", nullable: true),
                    AggregateTotalReturn = table.Column<decimal>(type: "TEXT", nullable: true),
                    AggregateMaxDrawdown = table.Column<decimal>(type: "TEXT", nullable: true),
                    AggregateSharpe = table.Column<decimal>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkForwardRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalkForwardWindows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<long>(type: "INTEGER", nullable: false),
                    WindowIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TrainTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TestFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TestTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BestParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    InSampleMetricValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    OutOfSampleTotalReturn = table.Column<decimal>(type: "TEXT", nullable: false),
                    OutOfSampleMaxDrawdown = table.Column<decimal>(type: "TEXT", nullable: false),
                    OutOfSampleSharpe = table.Column<decimal>(type: "TEXT", nullable: false),
                    OutOfSampleTradeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkForwardWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalkForwardWindows_WalkForwardRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "WalkForwardRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalkForwardWindows_RunId",
                table: "WalkForwardWindows",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WalkForwardWindows");
            migrationBuilder.DropTable(name: "WalkForwardRuns");
        }
    }
}
