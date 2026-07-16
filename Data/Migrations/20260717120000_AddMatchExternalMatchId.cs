using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchExternalMatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalMatchId",
                table: "Match",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Match_ExternalMatchId",
                table: "Match",
                column: "ExternalMatchId",
                unique: true,
                filter: "\"ExternalMatchId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Match_ExternalMatchId",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "ExternalMatchId",
                table: "Match");
        }
    }
}
