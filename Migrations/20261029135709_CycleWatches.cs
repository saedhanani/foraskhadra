using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class CycleWatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleWatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleWatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleWatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CycleWatches_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleWatches_OpportunityId",
                table: "CycleWatches",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleWatches_UserId_OpportunityId",
                table: "CycleWatches",
                columns: new[] { "UserId", "OpportunityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleWatches");
        }
    }
}
