using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinancialDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.CheckConstraint("CK_Budgets_Month", "[Month] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Budgets_Year", "[Year] BETWEEN 2000 AND 9999");
                });

            migrationBuilder.CreateTable(
                name: "FinancialGroups",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_FinancialGroups_FinancialGroupId",
                        column: x => x.FinancialGroupId,
                        principalSchema: "denarius",
                        principalTable: "FinancialGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reconciliations",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReconciledBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reconciliations_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "denarius",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_Currency", "LEN([Currency]) = 3");
                    table.ForeignKey(
                        name: "FK_Accounts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "denarius",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.CheckConstraint("CK_BudgetLines_Amount", "[Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalSchema: "denarius",
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "denarius",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.CheckConstraint("CK_JournalEntryLines_DebitCredit", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)");
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "denarius",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "denarius",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "denarius",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "FinancialGroups",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "Kind", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, 1, "Património e Poupanças", 1, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, 2, "Rendimentos Correntes", 2, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, 2, "Rendimentos Extra", 3, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, 3, "Despesas Correntes", 4, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, 3, "Despesas Extra", 5, null, null }
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "FinancialGroupId", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000001"), true, "Conta à Ordem", 1, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000001"), true, "Conta Poupança", 2, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000001"), true, "Investimentos", 3, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000001"), true, "Dinheiro", 4, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000002"), true, "Salário", 1, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000002"), true, "Subsídios", 2, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000002"), true, "Pensões", 3, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000003"), true, "Trabalhos ocasionais", 1, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000003"), true, "Reembolsos", 2, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000022"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000003"), true, "Prémios", 3, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000030"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Habitação", 1, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000031"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Água", 2, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000032"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Electricidade", 3, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000033"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Alimentação", 4, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000034"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Transportes", 5, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000035"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Saúde", 6, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000036"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Educação", 7, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000037"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Seguros", 8, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000038"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Lazer", 9, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000039"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000004"), true, "Subscrições", 10, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000050"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000005"), true, "Viagens", 1, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000051"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000005"), true, "Reparações", 2, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000052"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000005"), true, "Equipamentos", 3, null, null },
                    { new Guid("20000000-0000-0000-0000-000000000053"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, new Guid("10000000-0000-0000-0000-000000000005"), true, "Compras extraordinárias", 4, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CategoryId",
                schema: "denarius",
                table: "Accounts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Name",
                schema: "denarius",
                table: "Accounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_CategoryId",
                schema: "denarius",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_CategoryId",
                schema: "denarius",
                table: "BudgetLines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_Year_Month",
                schema: "denarius",
                table: "Budgets",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FinancialGroupId",
                schema: "denarius",
                table: "Categories",
                column: "FinancialGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FinancialGroupId_Name",
                schema: "denarius",
                table: "Categories",
                columns: new[] { "FinancialGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FinancialGroupId_SortOrder",
                schema: "denarius",
                table: "Categories",
                columns: new[] { "FinancialGroupId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGroups_Kind_SortOrder",
                schema: "denarius",
                table: "FinancialGroups",
                columns: new[] { "Kind", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGroups_Name",
                schema: "denarius",
                table: "FinancialGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Date",
                schema: "denarius",
                table: "JournalEntries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Status_Date",
                schema: "denarius",
                table: "JournalEntries",
                columns: new[] { "Status", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                schema: "denarius",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_CategoryId",
                schema: "denarius",
                table: "JournalEntryLines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                schema: "denarius",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reconciliations_JournalEntryId",
                schema: "denarius",
                table: "Reconciliations",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reconciliations_Status",
                schema: "denarius",
                table: "Reconciliations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLines",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "JournalEntryLines",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Reconciliations",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "JournalEntries",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "FinancialGroups",
                schema: "denarius");
        }
    }
}
