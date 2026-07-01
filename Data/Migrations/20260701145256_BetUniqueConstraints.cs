using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class BetUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BetResult_BetId",
                table: "BetResult");

            migrationBuilder.DropIndex(
                name: "IX_Bet_UserId",
                table: "Bet");

            migrationBuilder.CreateIndex(
                name: "Uq_BetResult_Bet",
                table: "BetResult",
                column: "BetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Uq_Bet_User_Match",
                table: "Bet",
                columns: new[] { "UserId", "MatchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Uq_BetResult_Bet",
                table: "BetResult");

            migrationBuilder.DropIndex(
                name: "Uq_Bet_User_Match",
                table: "Bet");

            migrationBuilder.CreateIndex(
                name: "IX_BetResult_BetId",
                table: "BetResult",
                column: "BetId");

            migrationBuilder.CreateIndex(
                name: "IX_Bet_UserId",
                table: "Bet",
                column: "UserId");
        }
    }
}
