using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class LastSeen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousVisitAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreviousVisitAt",
                table: "AspNetUsers");
        }
    }
}
