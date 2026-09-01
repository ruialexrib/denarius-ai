using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInsurancePortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsurancePolicies",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Insurer = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PaymentFrequency = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RenewalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InsuredSubject = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePremiums",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePremiums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurancePremiums_InsurancePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "denarius",
                        principalTable: "InsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsurancePremiums_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "denarius",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePremiumAttachments",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PremiumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePremiumAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurancePremiumAttachments_InsurancePremiums_PremiumId",
                        column: x => x.PremiumId,
                        principalSchema: "denarius",
                        principalTable: "InsurancePremiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_PolicyNumber",
                schema: "denarius",
                table: "InsurancePolicies",
                column: "PolicyNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_RenewalDate",
                schema: "denarius",
                table: "InsurancePolicies",
                column: "RenewalDate");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePremiumAttachments_PremiumId",
                schema: "denarius",
                table: "InsurancePremiumAttachments",
                column: "PremiumId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePremiums_DueDate",
                schema: "denarius",
                table: "InsurancePremiums",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePremiums_JournalEntryId",
                schema: "denarius",
                table: "InsurancePremiums",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePremiums_PolicyId",
                schema: "denarius",
                table: "InsurancePremiums",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsurancePremiumAttachments",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "InsurancePremiums",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "InsurancePolicies",
                schema: "denarius");
        }
    }
}
