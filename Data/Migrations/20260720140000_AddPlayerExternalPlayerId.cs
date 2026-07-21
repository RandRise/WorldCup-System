using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerExternalPlayerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalPlayerId",
                table: "Player",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_ExternalPlayerId",
                table: "Player",
                column: "ExternalPlayerId",
                unique: true,
                filter: "\"ExternalPlayerId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Player_ExternalPlayerId",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "ExternalPlayerId",
                table: "Player");
        }
    }
}
