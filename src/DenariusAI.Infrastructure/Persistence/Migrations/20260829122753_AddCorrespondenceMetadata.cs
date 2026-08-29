using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondenceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorrespondenceMetadata",
                schema: "denarius",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrespondenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrespondenceMetadata_Correspondence_CorrespondenceId",
                        column: x => x.CorrespondenceId,
                        principalSchema: "denarius",
                        principalTable: "Correspondence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceMetadata_CorrespondenceId",
                schema: "denarius",
                table: "CorrespondenceMetadata",
                column: "CorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceMetadata_CorrespondenceId_Key",
                schema: "denarius",
                table: "CorrespondenceMetadata",
                columns: new[] { "CorrespondenceId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrespondenceMetadata",
                schema: "denarius");
        }
    }
}
