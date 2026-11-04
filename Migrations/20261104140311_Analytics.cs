using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class Analytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PageViewDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageViewDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteEventDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteEventDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrafficSourceDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficSourceDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageViewDailyStats_Day_Path_Culture",
                table: "PageViewDailyStats",
                columns: new[] { "Day", "Path", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteEventDailyStats_Day_Event_Label",
                table: "SiteEventDailyStats",
                columns: new[] { "Day", "Event", "Label" },
                unique: true,
                filter: "[Label] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficSourceDailyStats_Day_Source",
                table: "TrafficSourceDailyStats",
                columns: new[] { "Day", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageViewDailyStats");

            migrationBuilder.DropTable(
                name: "SiteEventDailyStats");

            migrationBuilder.DropTable(
                name: "TrafficSourceDailyStats");
        }
    }
}
