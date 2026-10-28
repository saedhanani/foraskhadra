using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class FlagshipPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgramId",
                table: "Activities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FlagshipPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    MenuTitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MenuTitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MenuTitleFr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubtitleAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubtitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubtitleFr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeadlineValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeadlineLabelAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeadlineLabelEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeadlineLabelFr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntroAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntroEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntroFr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutcomesHeadingAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomesHeadingEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomesHeadingFr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomesAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomesEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomesFr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnersAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnersEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnersFr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimelineJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GalleryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagshipPrograms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ProgramId",
                table: "Activities",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FlagshipPrograms_Slug",
                table: "FlagshipPrograms",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_FlagshipPrograms_ProgramId",
                table: "Activities",
                column: "ProgramId",
                principalTable: "FlagshipPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_FlagshipPrograms_ProgramId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "FlagshipPrograms");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ProgramId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "Activities");
        }
    }
}
