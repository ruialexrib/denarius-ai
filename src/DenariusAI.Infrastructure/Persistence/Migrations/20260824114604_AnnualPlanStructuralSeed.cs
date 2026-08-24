using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnnualPlanStructuralSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "FinancialGroupId", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000001100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000001"), true, "Constituição de Poupanças", 5, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004100"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com a casa", 11, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004200"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com o carro e transportes", 12, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004300"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas Bancárias e Seguros", 13, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004400"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com o Estado e Impostos", 14, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004500"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com Compras", 15, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004600"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com cuidados pessoais", 16, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004700"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Despesas com Estudo e Formação", 17, null, null },
                    { new Guid("20000000-0000-0000-0000-000000004800"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Caixas e Fundo de Maneio", 18, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000001100"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004100"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004200"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004300"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004400"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004500"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004600"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004700"));

            migrationBuilder.DeleteData(
                schema: "denarius",
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000004800"));
        }
    }
}
