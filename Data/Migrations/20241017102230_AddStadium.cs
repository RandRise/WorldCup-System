using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStadium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_City_Countries_CountryId",
                table: "City");

            migrationBuilder.RenameIndex(
                name: "IX_City_CountryId",
                table: "City",
                newName: "IFK_City_Country_Id");

            migrationBuilder.CreateTable(
                name: "Stadium",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stadium", x => x.Id);
                    table.CheckConstraint("CK_Stadium_Name_Length_Less_Than_64", "Length(\"Name\") <= 64");
                    table.ForeignKey(
                        name: "FK_Stadium_City",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IFK_Stadium_City_Id",
                table: "Stadium",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "Uq_Stadium_Name",
                table: "Stadium",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_City_Country",
                table: "City",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_City_Country",
                table: "City");

            migrationBuilder.DropTable(
                name: "Stadium");

            migrationBuilder.RenameIndex(
                name: "IFK_City_Country_Id",
                table: "City",
                newName: "IX_City_CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_City_Countries_CountryId",
                table: "City",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
