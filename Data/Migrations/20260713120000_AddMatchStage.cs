using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "Match",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FeederMatchOneId",
                table: "Match",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeederMatchTwoId",
                table: "Match",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeederOneTakesLoser",
                table: "Match",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FeederTwoTakesLoser",
                table: "Match",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "TeamOneId",
                table: "Match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "TeamTwoId",
                table: "Match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Match_Stage",
                table: "Match",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_Match_FeederMatchOneId",
                table: "Match",
                column: "FeederMatchOneId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_FeederMatchTwoId",
                table: "Match",
                column: "FeederMatchTwoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Match_FeederMatchTwoId",
                table: "Match");

            migrationBuilder.DropIndex(
                name: "IX_Match_FeederMatchOneId",
                table: "Match");

            migrationBuilder.DropIndex(
                name: "IX_Match_Stage",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "FeederTwoTakesLoser",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "FeederOneTakesLoser",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "FeederMatchTwoId",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "FeederMatchOneId",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Match");

            migrationBuilder.AlterColumn<int>(
                name: "TeamOneId",
                table: "Match",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TeamTwoId",
                table: "Match",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
