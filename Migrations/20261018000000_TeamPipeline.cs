using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Foras_Khadra.Data;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <summary>
    /// أعمدة ربط الموقع بمنصة الفريق. هجرة يدوية بلا Designer:
    /// بعد تطبيقها شغّل `dotnet ef migrations add TeamPipelineSnapshot` لتحديث اللقطة (سيولّد هجرة فارغة).
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261018000000_TeamPipeline")]
    public partial class TeamPipeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "Status", table: "Opportunities", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<string>(name: "SourceUrl", table: "Opportunities", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PipeId", table: "Opportunities", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "AiGenerated", table: "Opportunities", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.CreateIndex(name: "IX_Opportunities_PipeId", table: "Opportunities", column: "PipeId");
            migrationBuilder.CreateIndex(name: "IX_Opportunities_Status", table: "Opportunities", column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Opportunities_Status", table: "Opportunities");
            migrationBuilder.DropIndex(name: "IX_Opportunities_PipeId", table: "Opportunities");
            migrationBuilder.DropColumn(name: "AiGenerated", table: "Opportunities");
            migrationBuilder.DropColumn(name: "PipeId", table: "Opportunities");
            migrationBuilder.DropColumn(name: "SourceUrl", table: "Opportunities");
            migrationBuilder.DropColumn(name: "Status", table: "Opportunities");
        }
    }
}
