using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssociateJournalEntriesWithBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BudgetId",
                schema: "denarius",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE entries
                SET [BudgetId] = budget.[Id]
                FROM [denarius].[JournalEntries] entries
                CROSS APPLY (
                    SELECT TOP (1) [Id]
                    FROM [denarius].[Budgets]
                    WHERE [Year] = 2026 AND [Month] = 7
                    ORDER BY [CreatedAt], [Id]
                ) budget
                WHERE entries.[Id] IN (
                    '40000000-0000-0000-0000-000000000001',
                    '40000000-0000-0000-0000-000000000002',
                    '40000000-0000-0000-0000-000000000003',
                    '40000000-0000-0000-0000-000000000004',
                    '40000000-0000-0000-0000-000000000005',
                    '40000000-0000-0000-0000-000000000006',
                    '40000000-0000-0000-0000-000000000007',
                    '40000000-0000-0000-0000-000000000008',
                    '40000000-0000-0000-0000-000000000009',
                    '40000000-0000-0000-0000-000000000010'
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BudgetId",
                schema: "denarius",
                table: "JournalEntries",
                column: "BudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Budgets_BudgetId",
                schema: "denarius",
                table: "JournalEntries",
                column: "BudgetId",
                principalSchema: "denarius",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Budgets_BudgetId",
                schema: "denarius",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_BudgetId",
                schema: "denarius",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                schema: "denarius",
                table: "JournalEntries");
        }
    }
}
