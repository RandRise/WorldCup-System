using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamRelationsWithoutFKIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Team_GroupId",
                table: "Team");

            migrationBuilder.RenameIndex(
                name: "IFK_Stadium_City_Id",
                table: "Stadium",
                newName: "IX_Stadium_CityId");

            migrationBuilder.RenameIndex(
                name: "Uq_Group_Name",
                table: "Group",
                newName: "IX_Group_WorldCupId");

            migrationBuilder.RenameIndex(
                name: "IFK_City_Country_Id",
                table: "City",
                newName: "IX_City_CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_GroupId",
                table: "Team",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Team_GroupId",
                table: "Team");

            migrationBuilder.RenameIndex(
                name: "IX_Stadium_CityId",
                table: "Stadium",
                newName: "IFK_Stadium_City_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Group_WorldCupId",
                table: "Group",
                newName: "Uq_Group_Name");

            migrationBuilder.RenameIndex(
                name: "IX_City_CountryId",
                table: "City",
                newName: "IFK_City_Country_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Team_GroupId",
                table: "Team",
                column: "GroupId",
                unique: true);
        }
    }
}
