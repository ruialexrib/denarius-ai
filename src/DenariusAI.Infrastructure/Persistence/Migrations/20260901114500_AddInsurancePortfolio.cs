using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations;

/// <summary>Adds insurance policies, premiums and premium attachments.</summary>
public partial class AddInsurancePortfolio : Migration
{
    /// <summary>Creates insurance portfolio tables and relationships.</summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "InsurancePolicies", schema: "denarius", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
            Insurer = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false), PolicyNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Type = table.Column<int>(type: "int", nullable: false), PaymentFrequency = table.Column<int>(type: "int", nullable: false), StartDate = table.Column<DateOnly>(type: "date", nullable: false),
            EndDate = table.Column<DateOnly>(type: "date", nullable: true), RenewalDate = table.Column<DateOnly>(type: "date", nullable: true), InsuredSubject = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
            Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true), Status = table.Column<int>(type: "int", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true), CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true), UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
        }, constraints: table => table.PrimaryKey("PK_InsurancePolicies", x => x.Id));
        migrationBuilder.CreateTable(name: "InsurancePremiums", schema: "denarius", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
            PeriodStart = table.Column<DateOnly>(type: "date", nullable: false), PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false), DueDate = table.Column<DateOnly>(type: "date", nullable: false), Reference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
            JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false), UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true), CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true), UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_InsurancePremiums", x => x.Id); table.ForeignKey("FK_InsurancePremiums_InsurancePolicies_PolicyId", x => x.PolicyId, "denarius", "InsurancePolicies", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_InsurancePremiums_JournalEntries_JournalEntryId", x => x.JournalEntryId, "denarius", "JournalEntries", "Id", onDelete: ReferentialAction.SetNull); });
        migrationBuilder.CreateTable(name: "InsurancePremiumAttachments", schema: "denarius", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PremiumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false), ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), DocumentBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false), UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true), CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true), UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_InsurancePremiumAttachments", x => x.Id); table.ForeignKey("FK_InsurancePremiumAttachments_InsurancePremiums_PremiumId", x => x.PremiumId, "denarius", "InsurancePremiums", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("IX_InsurancePolicies_PolicyNumber", "denarius", "InsurancePolicies", "PolicyNumber");
        migrationBuilder.CreateIndex("IX_InsurancePolicies_RenewalDate", "denarius", "InsurancePolicies", "RenewalDate");
        migrationBuilder.CreateIndex("IX_InsurancePremiums_DueDate", "denarius", "InsurancePremiums", "DueDate");
        migrationBuilder.CreateIndex("IX_InsurancePremiums_JournalEntryId", "denarius", "InsurancePremiums", "JournalEntryId");
        migrationBuilder.CreateIndex("IX_InsurancePremiums_PolicyId", "denarius", "InsurancePremiums", "PolicyId");
        migrationBuilder.CreateIndex("IX_InsurancePremiumAttachments_PremiumId", "denarius", "InsurancePremiumAttachments", "PremiumId");
    }

    /// <summary>Removes insurance portfolio tables.</summary>
    /// <param name="migrationBuilder">Migration builder.</param>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("InsurancePremiumAttachments", "denarius");
        migrationBuilder.DropTable("InsurancePremiums", "denarius");
        migrationBuilder.DropTable("InsurancePolicies", "denarius");
    }
}
