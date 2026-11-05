using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class Stage5Kpi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Opportunities");
        }
    }
}
