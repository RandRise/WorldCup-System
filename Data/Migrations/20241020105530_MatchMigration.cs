using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Match",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StadiumId = table.Column<int>(type: "integer", nullable: false),
                    TeamOneId = table.Column<int>(type: "integer", nullable: false),
                    TeamTwoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stadium_Match",
                        column: x => x.StadiumId,
                        principalTable: "Stadium",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamOne_Match",
                        column: x => x.TeamOneId,
                        principalTable: "Team",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamTwo_Match",
                        column: x => x.TeamTwoId,
                        principalTable: "Team",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Match_StadiumId",
                table: "Match",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_TeamOneId",
                table: "Match",
                column: "TeamOneId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_TeamTwoId",
                table: "Match",
                column: "TeamTwoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Match");
        }
    }
}
