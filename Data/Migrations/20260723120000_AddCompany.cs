using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations
{
    /// <summary>
    /// Phase 13 Task 2 — Company soft tenancy: Company table + User.CompanyId FK,
    /// unique InviteCode / optional Slug. Role CompanyAdmin is seeded in Program.cs.
    /// </summary>
    [DbContext(typeof(Data.Context.ApplicationDbContext))]
    [Migration("20260723120000_AddCompany")]
    public class AddCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                    table.CheckConstraint("CK_Company_Name_Length_Less_Than_128", "Length(\"Name\") <= 128");
                    table.CheckConstraint("CK_Company_InviteCode_Length_Less_Than_32", "Length(\"InviteCode\") <= 32");
                    table.CheckConstraint("CK_Company_Slug_Length_Less_Than_64", "\"Slug\" IS NULL OR Length(\"Slug\") <= 64");
                    table.ForeignKey(
                        name: "FK_Company_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Company_CreatedByUserId",
                table: "Company",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "Uq_Company_InviteCode",
                table: "Company",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Uq_Company_Slug",
                table: "Company",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Company",
                table: "AspNetUsers",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Company",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Company");
        }
    }
}
