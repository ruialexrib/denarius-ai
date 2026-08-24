using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DemonstrationAccountSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Accounts",
                columns: new[] { "Id", "AccountType", "CategoryId", "CreatedAt", "CreatedBy", "Currency", "Description", "InitialBalance", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), 1, new Guid("20000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", "Conta bancária de demonstração, sem movimentos financeiros.", 0m, true, "Conta à Ordem — Demonstração", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));
        }
    }
}
