using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentManagementAndLinkedReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SavingsCertificateId",
                schema: "denarius",
                table: "Reminders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyId",
                schema: "denarius",
                table: "Reminders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Correspondence",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correspondence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Supplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reminders",
                keyColumn: "Id",
                keyValue: new Guid("90000000-0000-0000-0000-000000000001"),
                columns: new[] { "SavingsCertificateId", "WarrantyId" },
                values: new object[] { null, null });

            migrationBuilder.Sql("""
                INSERT INTO [denarius].[Reminders]
                    ([Id], [Text], [EventDate], [NoticeDays], [SavingsCertificateId], [WarrantyId], [CreatedAt], [CreatedBy])
                SELECT NEWID(),
                    CONCAT(N'Capitalização do Certificado de Aforro ', [SeriesNumber], N': ', [Description]),
                    [NextCapitalization], 7, [Id], NULL, SYSUTCDATETIME(), N'system'
                FROM [denarius].[SavingsCertificates];
                """);

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reminders",
                keyColumn: "Id",
                keyValue: new Guid("90000000-0000-0000-0000-000000000002"),
                columns: new[] { "SavingsCertificateId", "WarrantyId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                schema: "denarius",
                table: "Reminders",
                keyColumn: "Id",
                keyValue: new Guid("90000000-0000-0000-0000-000000000003"),
                columns: new[] { "SavingsCertificateId", "WarrantyId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_SavingsCertificateId",
                schema: "denarius",
                table: "Reminders",
                column: "SavingsCertificateId",
                unique: true,
                filter: "[SavingsCertificateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_WarrantyId",
                schema: "denarius",
                table: "Reminders",
                column: "WarrantyId",
                unique: true,
                filter: "[WarrantyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondence_ReceivedDate",
                schema: "denarius",
                table: "Correspondence",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_ExpiryDate",
                schema: "denarius",
                table: "Warranties",
                column: "ExpiryDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_SavingsCertificates_SavingsCertificateId",
                schema: "denarius",
                table: "Reminders",
                column: "SavingsCertificateId",
                principalSchema: "denarius",
                principalTable: "SavingsCertificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Warranties_WarrantyId",
                schema: "denarius",
                table: "Reminders",
                column: "WarrantyId",
                principalSchema: "denarius",
                principalTable: "Warranties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_SavingsCertificates_SavingsCertificateId",
                schema: "denarius",
                table: "Reminders");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Warranties_WarrantyId",
                schema: "denarius",
                table: "Reminders");

            migrationBuilder.DropTable(
                name: "Correspondence",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Warranties",
                schema: "denarius");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_SavingsCertificateId",
                schema: "denarius",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_WarrantyId",
                schema: "denarius",
                table: "Reminders");

            migrationBuilder.Sql("DELETE FROM [denarius].[Reminders] WHERE [SavingsCertificateId] IS NOT NULL OR [WarrantyId] IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "SavingsCertificateId",
                schema: "denarius",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                schema: "denarius",
                table: "Reminders");
        }
    }
}
