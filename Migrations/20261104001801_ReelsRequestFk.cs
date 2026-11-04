using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class ReelsRequestFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReelsRequests_Opportunities_OpportunityId1",
                table: "ReelsRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReelsRequests_OpportunityId1",
                table: "ReelsRequests");

            migrationBuilder.DropColumn(
                name: "OpportunityId1",
                table: "ReelsRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpportunityId1",
                table: "ReelsRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReelsRequests_OpportunityId1",
                table: "ReelsRequests",
                column: "OpportunityId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ReelsRequests_Opportunities_OpportunityId1",
                table: "ReelsRequests",
                column: "OpportunityId1",
                principalTable: "Opportunities",
                principalColumn: "Id");
        }
    }
}
