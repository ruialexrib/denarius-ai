using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JournalEntryLineOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                schema: "denarius",
                table: "JournalEntryLines");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                schema: "denarius",
                table: "JournalEntryLines",
                column: "JournalEntryId",
                principalSchema: "denarius",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                schema: "denarius",
                table: "JournalEntryLines");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                schema: "denarius",
                table: "JournalEntryLines",
                column: "JournalEntryId",
                principalSchema: "denarius",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
