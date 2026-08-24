using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DemonstrationFinancialScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @BudgetId uniqueidentifier = (SELECT TOP (1) [Id] FROM [denarius].[Budgets] WHERE [Year] = 2026 AND [Month] = 7);
                IF @BudgetId IS NULL
                BEGIN
                    SET @BudgetId = NEWID();
                    INSERT INTO [denarius].[Budgets] ([Id], [Year], [Month], [CreatedAt], [CreatedBy])
                    VALUES (@BudgetId, 2026, 7, SYSDATETIMEOFFSET(), N'demo-seed');
                END;
                INSERT INTO [denarius].[BudgetLines] ([Id], [BudgetId], [CategoryId], [Amount], [CreatedAt], [CreatedBy])
                SELECT NEWID(), @BudgetId, source.[CategoryId], source.[Amount], SYSDATETIMEOFFSET(), N'demo-seed'
                FROM (VALUES
                    (CAST('20000000-0000-0000-0000-000000000030' AS uniqueidentifier), CAST(700 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000031' AS uniqueidentifier), CAST(35 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000032' AS uniqueidentifier), CAST(70 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000033' AS uniqueidentifier), CAST(250 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000034' AS uniqueidentifier), CAST(150 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000035' AS uniqueidentifier), CAST(75 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000037' AS uniqueidentifier), CAST(100 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000038' AS uniqueidentifier), CAST(60 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000039' AS uniqueidentifier), CAST(30 AS decimal(19,4))),
                    (CAST('20000000-0000-0000-0000-000000000050' AS uniqueidentifier), CAST(300 AS decimal(19,4)))
                ) source ([CategoryId], [Amount])
                WHERE NOT EXISTS (SELECT 1 FROM [denarius].[BudgetLines] existing WHERE existing.[BudgetId] = @BudgetId AND existing.[CategoryId] = source.[CategoryId]);
                """);
            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "Description",
                value: "Conta bancária principal do cenário de demonstração.");

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Accounts",
                columns: new[] { "Id", "AccountType", "CategoryId", "CreatedAt", "CreatedBy", "Currency", "Description", "InitialBalance", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000002"), 3, new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", "Poupança familiar do cenário de demonstração.", 500m, true, "Conta Poupança — Demonstração", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), 2, new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", "Carteira de numerário do cenário de demonstração.", 100m, true, "Dinheiro — Demonstração", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000004"), 7, new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", "Contrapartida contabilística dos rendimentos.", 0m, true, "Rendimentos — Demonstração", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000005"), 8, new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", "Contrapartida contabilística das despesas.", 0m, true, "Despesas — Demonstração", null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "JournalEntries",
                columns: new[] { "Id", "CancelledAt", "CancelledBy", "CreatedAt", "CreatedBy", "Date", "Description", "Notes", "Reference", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 1), "Salário mensal", "Dados de demonstração — julho 2026", "REC-JUL-001", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000002"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 3), "Renda da casa", "Dados de demonstração — julho 2026", "PAG-JUL-001", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000003"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 5), "Compras de supermercado", "Dados de demonstração — julho 2026", "TALAO-1842", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000004"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 7), "Fatura de eletricidade", "Dados de demonstração — julho 2026", "ELEC-0726", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000005"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 8), "Fatura de água", "Dados de demonstração — julho 2026", "AGUA-0726", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000006"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 10), "Transferência para poupança", "Dados de demonstração — julho 2026", "TRF-POUP", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000007"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 12), "Levantamento ATM", "Dados de demonstração — julho 2026", "ATM-1208", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000008"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 15), "Jantar em família", "Dados de demonstração — julho 2026", "REST-1508", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000009"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 18), "Trabalho ocasional", "Dados de demonstração — julho 2026", "FREELANCE-07", 1, null, null },
                    { new Guid("40000000-0000-0000-0000-000000000010"), null, null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 7, 20), "Viagem de verão", "Dados de demonstração — julho 2026", "VIAGEM-2026", 1, null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "JournalEntryLines",
                columns: new[] { "Id", "AccountId", "CategoryId", "CreatedAt", "CreatedBy", "Credit", "Debit", "Description", "JournalEntryId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 2500m, null, new Guid("40000000-0000-0000-0000-000000000001"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2500m, 0m, null, new Guid("40000000-0000-0000-0000-000000000001"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 750m, null, new Guid("40000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 750m, 0m, null, new Guid("40000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 180m, null, new Guid("40000000-0000-0000-0000-000000000003"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 180m, 0m, null, new Guid("40000000-0000-0000-0000-000000000003"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 65m, null, new Guid("40000000-0000-0000-0000-000000000004"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 65m, 0m, null, new Guid("40000000-0000-0000-0000-000000000004"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000009"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 32m, null, new Guid("40000000-0000-0000-0000-000000000005"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000010"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 32m, 0m, null, new Guid("40000000-0000-0000-0000-000000000005"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000002"), new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 300m, null, new Guid("40000000-0000-0000-0000-000000000006"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 300m, 0m, null, new Guid("40000000-0000-0000-0000-000000000006"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000013"), new Guid("30000000-0000-0000-0000-000000000003"), new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 100m, null, new Guid("40000000-0000-0000-0000-000000000007"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000014"), new Guid("30000000-0000-0000-0000-000000000001"), new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 100m, 0m, null, new Guid("40000000-0000-0000-0000-000000000007"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000015"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 80m, null, new Guid("40000000-0000-0000-0000-000000000008"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000016"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 80m, 0m, null, new Guid("40000000-0000-0000-0000-000000000008"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000017"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 350m, null, new Guid("40000000-0000-0000-0000-000000000009"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000018"), new Guid("30000000-0000-0000-0000-000000000004"), new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 350m, 0m, null, new Guid("40000000-0000-0000-0000-000000000009"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000019"), new Guid("30000000-0000-0000-0000-000000000005"), new Guid("20000000-0000-0000-0000-000000000050"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 0m, 450m, null, new Guid("40000000-0000-0000-0000-000000000010"), null, null },
                    { new Guid("50000000-0000-0000-0000-000000000020"), new Guid("30000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 450m, 0m, null, new Guid("40000000-0000-0000-0000-000000000010"), null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Reconciliations",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "JournalEntryId", "ReconciledAt", "ReconciledBy", "Status", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("80000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null },
                    { new Guid("80000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", 2, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [denarius].[BudgetLines] WHERE [CreatedBy] = N'demo-seed';
                DELETE FROM [denarius].[Budgets] WHERE [CreatedBy] = N'demo-seed' AND NOT EXISTS (
                    SELECT 1 FROM [denarius].[BudgetLines] WHERE [BudgetId] = [denarius].[Budgets].[Id]
                );
                """);
            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntryLines",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Reconciliations",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "JournalEntries",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"));

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "Description",
                value: "Conta bancária de demonstração, sem movimentos financeiros.");
        }
    }
}
