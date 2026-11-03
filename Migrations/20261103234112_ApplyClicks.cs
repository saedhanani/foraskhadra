using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class ApplyClicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpportunityDailyStats",
                columns: table => new
                {
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    ApplyClicks = table.Column<int>(type: "int", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityDailyStats", x => new { x.OpportunityId, x.Day });
                    table.ForeignKey(
                        name: "FK_OpportunityDailyStats_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityDailyStats");
        }
    }
}
