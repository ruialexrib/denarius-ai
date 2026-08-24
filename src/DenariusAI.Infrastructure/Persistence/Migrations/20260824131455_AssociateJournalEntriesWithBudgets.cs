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

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"),
                column: "BudgetId",
                value: new Guid("60000000-0000-0000-0000-000000000001"));

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
