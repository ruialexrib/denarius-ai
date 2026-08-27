using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetBalancesWidgetPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowAssetBalancesWidget",
                schema: "denarius",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowAssetBalancesWidget",
                schema: "denarius",
                table: "AspNetUsers");
        }
    }
}
