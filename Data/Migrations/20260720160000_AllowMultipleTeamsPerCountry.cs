using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <summary>
    /// Phase 12 Task 1 — allow the same country to have one Team per World Cup
    /// (scoped via Group.WorldCupId). Drops the global unique index on Team.CountryId.
    /// </summary>
    [DbContext(typeof(Data.Context.ApplicationDbContext))]
    [Migration("20260720160000_AllowMultipleTeamsPerCountry")]
    public class AllowMultipleTeamsPerCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Team_CountryId",
                table: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_Team_CountryId",
                table: "Team",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Team_CountryId",
                table: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_Team_CountryId",
                table: "Team",
                column: "CountryId",
                unique: true);
        }
    }
}
