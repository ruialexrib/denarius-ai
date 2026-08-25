using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserNoticeAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CookieConsentAcceptedAt",
                schema: "denarius",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DemonstrationDataAcknowledgedAt",
                schema: "denarius",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CookieConsentAcceptedAt",
                schema: "denarius",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DemonstrationDataAcknowledgedAt",
                schema: "denarius",
                table: "AspNetUsers");
        }
    }
}
