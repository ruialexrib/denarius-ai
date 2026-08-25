using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDemonstrationScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "InitialBalance",
                value: 1850m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                column: "InitialBalance",
                value: 4200m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                column: "InitialBalance",
                value: 120m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 1), "Dados de demonstração — 01/2026", "SAL-2026-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 3), "Dados de demonstração — 01/2026", "RENDA-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 6), "Dados de demonstração — 01/2026", "SUPER-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 8), "Dados de demonstração — 01/2026", "ELEC-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 9), "Dados de demonstração — 01/2026", "AGUA-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 12), "Passe e combustível", "Dados de demonstração — 01/2026", "TRANSP-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 15), "Transferência para poupança", "Dados de demonstração — 01/2026", "POUP-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 20), "Lazer em família", "Dados de demonstração — 01/2026", "LAZER-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 1, 24), "Dados de demonstração — 01/2026", "EXTRA-01" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { null, new DateOnly(2026, 2, 1), "Salário mensal", "Dados de demonstração — 02/2026", "SAL-2026-02" });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "JournalEntries",
                columns: new[] { "Id", "BudgetId", "CancelledAt", "CancelledBy", "CreatedAt", "CreatedBy", "Date", "Description", "Notes", "Reference", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000011"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 3), "Renda da casa", "Dados de demonstração — 02/2026", "RENDA-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000012"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 6), "Compras de supermercado", "Dados de demonstração — 02/2026", "SUPER-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000013"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 8), "Fatura de eletricidade", "Dados de demonstração — 02/2026", "ELEC-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000014"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 9), "Fatura de água", "Dados de demonstração — 02/2026", "AGUA-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000015"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 12), "Passe e combustível", "Dados de demonstração — 02/2026", "TRANSP-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000016"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 15), "Transferência para poupança", "Dados de demonstração — 02/2026", "POUP-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000017"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 20), "Lazer em família", "Dados de demonstração — 02/2026", "LAZER-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000018"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 2, 24), "Trabalho ocasional", "Dados de demonstração — 02/2026", "EXTRA-02", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000019"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 1), "Salário mensal", "Dados de demonstração — 03/2026", "SAL-2026-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000020"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 3), "Renda da casa", "Dados de demonstração — 03/2026", "RENDA-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000021"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 6), "Compras de supermercado", "Dados de demonstração — 03/2026", "SUPER-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000022"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 8), "Fatura de eletricidade", "Dados de demonstração — 03/2026", "ELEC-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000023"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 9), "Fatura de água", "Dados de demonstração — 03/2026", "AGUA-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000024"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 12), "Passe e combustível", "Dados de demonstração — 03/2026", "TRANSP-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000025"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 15), "Transferência para poupança", "Dados de demonstração — 03/2026", "POUP-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000026"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 20), "Lazer em família", "Dados de demonstração — 03/2026", "LAZER-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000027"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 3, 24), "Trabalho ocasional", "Dados de demonstração — 03/2026", "EXTRA-03", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000028"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 1), "Salário mensal", "Dados de demonstração — 04/2026", "SAL-2026-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000029"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 3), "Renda da casa", "Dados de demonstração — 04/2026", "RENDA-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000030"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 6), "Compras de supermercado", "Dados de demonstração — 04/2026", "SUPER-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000031"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 8), "Fatura de eletricidade", "Dados de demonstração — 04/2026", "ELEC-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000032"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 9), "Fatura de água", "Dados de demonstração — 04/2026", "AGUA-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000033"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 12), "Passe e combustível", "Dados de demonstração — 04/2026", "TRANSP-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000034"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 15), "Transferência para poupança", "Dados de demonstração — 04/2026", "POUP-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000035"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 20), "Lazer em família", "Dados de demonstração — 04/2026", "LAZER-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000036"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 4, 24), "Trabalho ocasional", "Dados de demonstração — 04/2026", "EXTRA-04", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000037"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 1), "Salário mensal", "Dados de demonstração — 05/2026", "SAL-2026-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000038"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 3), "Renda da casa", "Dados de demonstração — 05/2026", "RENDA-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000039"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 6), "Compras de supermercado", "Dados de demonstração — 05/2026", "SUPER-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000040"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 8), "Fatura de eletricidade", "Dados de demonstração — 05/2026", "ELEC-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000041"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 9), "Fatura de água", "Dados de demonstração — 05/2026", "AGUA-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000042"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 12), "Passe e combustível", "Dados de demonstração — 05/2026", "TRANSP-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000043"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 15), "Transferência para poupança", "Dados de demonstração — 05/2026", "POUP-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000044"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 20), "Lazer em família", "Dados de demonstração — 05/2026", "LAZER-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000045"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 5, 24), "Trabalho ocasional", "Dados de demonstração — 05/2026", "EXTRA-05", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000046"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 1), "Salário mensal", "Dados de demonstração — 06/2026", "SAL-2026-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000047"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 3), "Renda da casa", "Dados de demonstração — 06/2026", "RENDA-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000048"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 6), "Compras de supermercado", "Dados de demonstração — 06/2026", "SUPER-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000049"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 8), "Fatura de eletricidade", "Dados de demonstração — 06/2026", "ELEC-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000050"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 9), "Fatura de água", "Dados de demonstração — 06/2026", "AGUA-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000051"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 12), "Passe e combustível", "Dados de demonstração — 06/2026", "TRANSP-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000052"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 15), "Transferência para poupança", "Dados de demonstração — 06/2026", "POUP-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000053"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 20), "Lazer em família", "Dados de demonstração — 06/2026", "LAZER-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000054"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 6, 24), "Trabalho ocasional", "Dados de demonstração — 06/2026", "EXTRA-06", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000055"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 1), "Salário mensal", "Dados de demonstração — 07/2026", "SAL-2026-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000056"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 3), "Renda da casa", "Dados de demonstração — 07/2026", "RENDA-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000057"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 6), "Compras de supermercado", "Dados de demonstração — 07/2026", "SUPER-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000058"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 8), "Fatura de eletricidade", "Dados de demonstração — 07/2026", "ELEC-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000059"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 9), "Fatura de água", "Dados de demonstração — 07/2026", "AGUA-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000060"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 12), "Passe e combustível", "Dados de demonstração — 07/2026", "TRANSP-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000061"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 15), "Transferência para poupança", "Dados de demonstração — 07/2026", "POUP-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000062"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 20), "Lazer em família", "Dados de demonstração — 07/2026", "LAZER-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000063"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 24), "Trabalho ocasional", "Dados de demonstração — 07/2026", "EXTRA-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000064"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 1), "Salário mensal", "Dados de demonstração — 08/2026", "SAL-2026-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000065"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 3), "Renda da casa", "Dados de demonstração — 08/2026", "RENDA-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000066"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 6), "Compras de supermercado", "Dados de demonstração — 08/2026", "SUPER-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000067"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 8), "Fatura de eletricidade", "Dados de demonstração — 08/2026", "ELEC-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000068"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 9), "Fatura de água", "Dados de demonstração — 08/2026", "AGUA-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000069"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 12), "Passe e combustível", "Dados de demonstração — 08/2026", "TRANSP-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000070"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 15), "Transferência para poupança", "Dados de demonstração — 08/2026", "POUP-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000071"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 20), "Lazer em família", "Dados de demonstração — 08/2026", "LAZER-08", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000072"), null, null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 24), "Trabalho ocasional", "Dados de demonstração — 08/2026", "EXTRA-08", 1, null, null }
                });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"),
                column: "Debit",
                value: 2650m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"),
                column: "Credit",
                value: 2650m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"),
                column: "Debit",
                value: 780m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"),
                column: "Credit",
                value: 780m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"),
                column: "Debit",
                value: 214m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000006"),
                column: "Credit",
                value: 214m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000007"),
                column: "Debit",
                value: 63m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000008"),
                column: "Credit",
                value: 63m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000009"),
                column: "Debit",
                value: 29m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000010"),
                column: "Credit",
                value: 29m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000011"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), 95m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000012"),
                columns: new[] { "CategoryId", "Credit" },
                values: new object[] { null, 95m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000013"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), 250m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000014"),
                columns: new[] { "CategoryId", "Credit" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000001100"), 250m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000015"),
                column: "Debit",
                value: 73m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000016"),
                column: "Credit",
                value: 73m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000017"),
                column: "Debit",
                value: 190m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000018"),
                column: "Credit",
                value: 190m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000019"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), null, 2650m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000020"),
                columns: new[] { "AccountId", "CategoryId", "Credit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), 2650m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000003"),
                column: "JournalEntryId",
                value: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000004"),
                column: "JournalEntryId",
                value: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Reconciliations",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "JournalEntryId", "ReconciledAt", "ReconciledBy", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("80000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "JournalEntryLines",
                columns: new[] { "Id", "AccountId", "CategoryId", "CreatedAt", "CreatedBy", "Credit", "Debit", "Description", "JournalEntryId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000021"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000011"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000022"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000011"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000023"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 218m, null, new Guid("40000000-0000-0000-0000-000000000012"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000024"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 218m, 0m, null, new Guid("40000000-0000-0000-0000-000000000012"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000025"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 64m, null, new Guid("40000000-0000-0000-0000-000000000013"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000026"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 64m, 0m, null, new Guid("40000000-0000-0000-0000-000000000013"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000027"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 30m, null, new Guid("40000000-0000-0000-0000-000000000014"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000028"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 30m, 0m, null, new Guid("40000000-0000-0000-0000-000000000014"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000029"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000015"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000030"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000015"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000031"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000016"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000032"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000016"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000033"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 76m, null, new Guid("40000000-0000-0000-0000-000000000017"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000034"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 76m, 0m, null, new Guid("40000000-0000-0000-0000-000000000017"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000035"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 200m, null, new Guid("40000000-0000-0000-0000-000000000018"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000036"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 200m, 0m, null, new Guid("40000000-0000-0000-0000-000000000018"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000037"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000019"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000038"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000019"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000039"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000020"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000040"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000020"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000041"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 222m, null, new Guid("40000000-0000-0000-0000-000000000021"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000042"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 222m, 0m, null, new Guid("40000000-0000-0000-0000-000000000021"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000043"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 65m, null, new Guid("40000000-0000-0000-0000-000000000022"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000044"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 65m, 0m, null, new Guid("40000000-0000-0000-0000-000000000022"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000045"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 31m, null, new Guid("40000000-0000-0000-0000-000000000023"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000046"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 31m, 0m, null, new Guid("40000000-0000-0000-0000-000000000023"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000047"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000024"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000048"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000024"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000049"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000025"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000050"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000025"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000051"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 79m, null, new Guid("40000000-0000-0000-0000-000000000026"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000052"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 79m, 0m, null, new Guid("40000000-0000-0000-0000-000000000026"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000053"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 210m, null, new Guid("40000000-0000-0000-0000-000000000027"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000054"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 210m, 0m, null, new Guid("40000000-0000-0000-0000-000000000027"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000055"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000028"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000056"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000028"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000057"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000029"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000058"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000029"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000059"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 226m, null, new Guid("40000000-0000-0000-0000-000000000030"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000060"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 226m, 0m, null, new Guid("40000000-0000-0000-0000-000000000030"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000061"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 66m, null, new Guid("40000000-0000-0000-0000-000000000031"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000062"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 66m, 0m, null, new Guid("40000000-0000-0000-0000-000000000031"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000063"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 32m, null, new Guid("40000000-0000-0000-0000-000000000032"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000064"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 32m, 0m, null, new Guid("40000000-0000-0000-0000-000000000032"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000065"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000033"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000066"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000033"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000067"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000034"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000068"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000034"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000069"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 82m, null, new Guid("40000000-0000-0000-0000-000000000035"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000070"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 82m, 0m, null, new Guid("40000000-0000-0000-0000-000000000035"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000071"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 220m, null, new Guid("40000000-0000-0000-0000-000000000036"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000072"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 220m, 0m, null, new Guid("40000000-0000-0000-0000-000000000036"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000073"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000037"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000074"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000037"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000075"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000038"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000076"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000038"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000077"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 230m, null, new Guid("40000000-0000-0000-0000-000000000039"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000078"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 230m, 0m, null, new Guid("40000000-0000-0000-0000-000000000039"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000079"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 67m, null, new Guid("40000000-0000-0000-0000-000000000040"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000080"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 67m, 0m, null, new Guid("40000000-0000-0000-0000-000000000040"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000081"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 33m, null, new Guid("40000000-0000-0000-0000-000000000041"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000082"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 33m, 0m, null, new Guid("40000000-0000-0000-0000-000000000041"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000083"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000042"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000084"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000042"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000085"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000043"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000086"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000043"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000087"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 85m, null, new Guid("40000000-0000-0000-0000-000000000044"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000088"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 85m, 0m, null, new Guid("40000000-0000-0000-0000-000000000044"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000089"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 230m, null, new Guid("40000000-0000-0000-0000-000000000045"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000090"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 230m, 0m, null, new Guid("40000000-0000-0000-0000-000000000045"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000091"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000046"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000092"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000046"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000093"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000047"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000094"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000047"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000095"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 234m, null, new Guid("40000000-0000-0000-0000-000000000048"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000096"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 234m, 0m, null, new Guid("40000000-0000-0000-0000-000000000048"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000097"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 68m, null, new Guid("40000000-0000-0000-0000-000000000049"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000098"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 68m, 0m, null, new Guid("40000000-0000-0000-0000-000000000049"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000099"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 34m, null, new Guid("40000000-0000-0000-0000-000000000050"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000100"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 34m, 0m, null, new Guid("40000000-0000-0000-0000-000000000050"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000101"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000051"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000102"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000051"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000103"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000052"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000104"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000052"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000105"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 88m, null, new Guid("40000000-0000-0000-0000-000000000053"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000106"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 88m, 0m, null, new Guid("40000000-0000-0000-0000-000000000053"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000107"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 240m, null, new Guid("40000000-0000-0000-0000-000000000054"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000108"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 240m, 0m, null, new Guid("40000000-0000-0000-0000-000000000054"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000109"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000055"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000110"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000055"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000111"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000056"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000112"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000056"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000113"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 238m, null, new Guid("40000000-0000-0000-0000-000000000057"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000114"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 238m, 0m, null, new Guid("40000000-0000-0000-0000-000000000057"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000115"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 69m, null, new Guid("40000000-0000-0000-0000-000000000058"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000116"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 69m, 0m, null, new Guid("40000000-0000-0000-0000-000000000058"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000117"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 35m, null, new Guid("40000000-0000-0000-0000-000000000059"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000118"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 35m, 0m, null, new Guid("40000000-0000-0000-0000-000000000059"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000119"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000060"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000120"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000060"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000121"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000061"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000122"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000061"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000123"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 91m, null, new Guid("40000000-0000-0000-0000-000000000062"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000124"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 91m, 0m, null, new Guid("40000000-0000-0000-0000-000000000062"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000125"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000063"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000126"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000063"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000127"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2650m, null, new Guid("40000000-0000-0000-0000-000000000064"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000128"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2650m, 0m, null, new Guid("40000000-0000-0000-0000-000000000064"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000129"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 780m, null, new Guid("40000000-0000-0000-0000-000000000065"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000130"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 780m, 0m, null, new Guid("40000000-0000-0000-0000-000000000065"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000131"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 242m, null, new Guid("40000000-0000-0000-0000-000000000066"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000132"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 242m, 0m, null, new Guid("40000000-0000-0000-0000-000000000066"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000133"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 70m, null, new Guid("40000000-0000-0000-0000-000000000067"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000134"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 70m, 0m, null, new Guid("40000000-0000-0000-0000-000000000067"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000135"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 36m, null, new Guid("40000000-0000-0000-0000-000000000068"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000136"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 36m, 0m, null, new Guid("40000000-0000-0000-0000-000000000068"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000137"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 95m, null, new Guid("40000000-0000-0000-0000-000000000069"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000138"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 95m, 0m, null, new Guid("40000000-0000-0000-0000-000000000069"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000139"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 250m, null, new Guid("40000000-0000-0000-0000-000000000070"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000140"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 250m, 0m, null, new Guid("40000000-0000-0000-0000-000000000070"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000141"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 94m, null, new Guid("40000000-0000-0000-0000-000000000071"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000142"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 94m, 0m, null, new Guid("40000000-0000-0000-0000-000000000071"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000143"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 260m, null, new Guid("40000000-0000-0000-0000-000000000072"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000144"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 260m, 0m, null, new Guid("40000000-0000-0000-0000-000000000072"), null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Reconciliations",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "JournalEntryId", "ReconciledAt", "ReconciledBy", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("80000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 9, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000013"), new DateTimeOffset(new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000014"), new DateTimeOffset(new DateTime(2026, 9, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000015"), new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000013"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000019"), new DateTimeOffset(new DateTime(2026, 9, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000014"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000015"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(2026, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000016"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000022"), new DateTimeOffset(new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000017"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000023"), new DateTimeOffset(new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000018"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000024"), new DateTimeOffset(new DateTime(2026, 9, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000019"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000028"), new DateTimeOffset(new DateTime(2026, 9, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000029"), new DateTimeOffset(new DateTime(2026, 9, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 9, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000022"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000023"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 9, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000024"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 9, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000025"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000037"), new DateTimeOffset(new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000026"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 9, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000027"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000039"), new DateTimeOffset(new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000028"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000040"), new DateTimeOffset(new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000029"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000041"), new DateTimeOffset(new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000042"), new DateTimeOffset(new DateTime(2026, 9, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000046"), new DateTimeOffset(new DateTime(2026, 9, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000047"), new DateTimeOffset(new DateTime(2026, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000048"), new DateTimeOffset(new DateTime(2026, 9, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000049"), new DateTimeOffset(new DateTime(2026, 9, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000035"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000050"), new DateTimeOffset(new DateTime(2026, 9, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000036"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000051"), new DateTimeOffset(new DateTime(2026, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000037"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000055"), new DateTimeOffset(new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000056"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000039"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000057"), new DateTimeOffset(new DateTime(2026, 10, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000040"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000058"), new DateTimeOffset(new DateTime(2026, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000041"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000059"), new DateTimeOffset(new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000042"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000060"), new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000043"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000064"), new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000044"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000065"), new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000045"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000066"), new DateTimeOffset(new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000046"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000067"), new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000047"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000068"), new DateTimeOffset(new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000048"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000069"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000037"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000046"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000047"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000048"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000049"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000054"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000055"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000056"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000057"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000058"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000059"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000062"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000063"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000064"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000065"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000066"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000067"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000068"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000069"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000070"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000071"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000072"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000073"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000074"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000075"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000076"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000077"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000078"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000079"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000080"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000081"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000082"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000083"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000084"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000085"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000086"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000087"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000088"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000089"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000090"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000091"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000092"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000093"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000094"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000095"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000096"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000097"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000098"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000099"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000100"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000101"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000102"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000103"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000104"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000105"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000106"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000107"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000108"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000109"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000110"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000111"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000112"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000113"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000114"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000115"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000116"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000117"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000118"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000119"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000120"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000121"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000122"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000123"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000124"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000125"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000126"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000127"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000128"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000129"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000130"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000131"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000132"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000133"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000134"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000135"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000136"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000137"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000138"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000139"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000140"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000141"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000142"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000143"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000144"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000037"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000046"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000047"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000048"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000037"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000046"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000047"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000048"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000049"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000051"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000054"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000055"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000056"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000057"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000058"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000059"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000062"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000063"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000064"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000065"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000066"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000067"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000068"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000069"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000070"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000071"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000072"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "InitialBalance",
                value: 0m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                column: "InitialBalance",
                value: 500m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                column: "InitialBalance",
                value: 100m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 1), "Dados de demonstração — julho 2026", "REC-JUL-001" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 3), "Dados de demonstração — julho 2026", "PAG-JUL-001" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 5), "Dados de demonstração — julho 2026", "TALAO-1842" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 7), "Dados de demonstração — julho 2026", "ELEC-0726" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 8), "Dados de demonstração — julho 2026", "AGUA-0726" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 10), "Transferência para poupança", "Dados de demonstração — julho 2026", "TRF-POUP" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 12), "Levantamento ATM", "Dados de demonstração — julho 2026", "ATM-1208" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 15), "Jantar em família", "Dados de demonstração — julho 2026", "REST-1508" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "BudgetId", "Date", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 18), "Dados de demonstração — julho 2026", "FREELANCE-07" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"),
                columns: new[] { "BudgetId", "Date", "Description", "Notes", "Reference" },
                values: new object[] { new Guid("60000000-0000-0000-0000-000000000001"), new DateOnly(2026, 7, 20), "Viagem de verão", "Dados de demonstração — julho 2026", "VIAGEM-2026" });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"),
                column: "Debit",
                value: 2500m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"),
                column: "Credit",
                value: 2500m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"),
                column: "Debit",
                value: 750m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"),
                column: "Credit",
                value: 750m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"),
                column: "Debit",
                value: 180m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000006"),
                column: "Credit",
                value: 180m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000007"),
                column: "Debit",
                value: 65m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000008"),
                column: "Credit",
                value: 65m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000009"),
                column: "Debit",
                value: 32m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000010"),
                column: "Credit",
                value: 32m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000011"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), 300m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000012"),
                columns: new[] { "CategoryId", "Credit" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000001100"), 300m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000013"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000003"), new Guid("20000000-0000-0000-0000-000000000004"), 100m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000014"),
                columns: new[] { "CategoryId", "Credit" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000004"), 100m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000015"),
                column: "Debit",
                value: 80m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000016"),
                column: "Credit",
                value: 80m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000017"),
                column: "Debit",
                value: 350m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000018"),
                column: "Credit",
                value: 350m);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000019"),
                columns: new[] { "AccountId", "CategoryId", "Debit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000050"), 450m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000020"),
                columns: new[] { "AccountId", "CategoryId", "Credit" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), null, 450m });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000003"),
                column: "JournalEntryId",
                value: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000004"),
                column: "JournalEntryId",
                value: new Guid("40000000-0000-0000-0000-000000000005"));
        }
    }
}
