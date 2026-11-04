using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class EligibilityProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EligibilityProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResidenceIso = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    NationalityIso = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    BirthYear = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FieldKeys = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    GraduationYear = table.Column<int>(type: "int", nullable: true),
                    University = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EnglishTests = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    EnglishScore = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FrenchCertificate = table.Column<bool>(type: "bit", nullable: false),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Goals = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HostCountries = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullyFundedOnly = table.Column<bool>(type: "bit", nullable: false),
                    RemoteOnly = table.Column<bool>(type: "bit", nullable: false),
                    StartWithinMonths = table.Column<int>(type: "int", nullable: true),
                    Documents = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityProfiles_UserId",
                table: "EligibilityProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EligibilityProfiles");
        }
    }
}
