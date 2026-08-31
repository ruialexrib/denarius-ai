using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureStockMarketAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForecastEnabled",
                schema: "denarius",
                table: "StockPositions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HistoryStartDate",
                schema: "denarius",
                table: "StockPositions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql(
                "UPDATE [denarius].[StockPositions] SET [HistoryStartDate] = DATEADD(year, -2, [PriceDate]) WHERE [HistoryStartDate] = '0001-01-01'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForecastEnabled",
                schema: "denarius",
                table: "StockPositions");

            migrationBuilder.DropColumn(
                name: "HistoryStartDate",
                schema: "denarius",
                table: "StockPositions");
        }
    }
}
