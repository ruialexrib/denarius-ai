using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reminders",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NoticeDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderAcknowledgements",
                schema: "denarius",
                columns: table => new
                {
                    ReminderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderAcknowledgements", x => new { x.ReminderId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ReminderAcknowledgements_Reminders_ReminderId",
                        column: x => x.ReminderId,
                        principalSchema: "denarius",
                        principalTable: "Reminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "denarius",
                table: "Reminders",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EventDate", "NoticeDays", "Text", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("90000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 8, 28), 7, "Confirmar a próxima capitalização dos Certificados de Aforro", null, null },
                    { new Guid("90000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 9, 15), 15, "Rever e renovar o seguro automóvel", null, null },
                    { new Guid("90000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "demo-seed", new DateOnly(2026, 12, 15), 30, "Preparar o orçamento familiar do próximo ano", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderAcknowledgements_UserId",
                schema: "denarius",
                table: "ReminderAcknowledgements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_EventDate",
                schema: "denarius",
                table: "Reminders",
                column: "EventDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReminderAcknowledgements",
                schema: "denarius");

            migrationBuilder.DropTable(
                name: "Reminders",
                schema: "denarius");
        }
    }
}
