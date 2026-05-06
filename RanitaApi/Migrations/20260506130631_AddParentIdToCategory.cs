using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RanitaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddParentIdToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetCode",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetCodeExpiresAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentId",
                table: "Categories",
                column: "ParentId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ResetCode",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ResetCodeExpiresAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Categories");
        }
    }
}
