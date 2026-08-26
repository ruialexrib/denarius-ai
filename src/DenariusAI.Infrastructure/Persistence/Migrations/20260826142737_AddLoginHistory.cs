using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginHistory",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LoggedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "denarius",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_UserId_LoggedInAt",
                schema: "denarius",
                table: "LoginHistory",
                columns: new[] { "UserId", "LoggedInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginHistory",
                schema: "denarius");
        }
    }
}
