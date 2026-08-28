using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleProfileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageBase64",
                schema: "denarius",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageContentType",
                schema: "denarius",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageBase64",
                schema: "denarius",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImageContentType",
                schema: "denarius",
                table: "AspNetUsers");
        }
    }
}
