using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class Stage4Growth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSponsored",
                table: "Opportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SponsoredUntil",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AmbassadorTaskSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TaskKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Month = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProofUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbassadorTaskSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmbassadorTaskSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationGuides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramKey = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleFr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SummaryAr = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SummaryEn = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SummaryFr = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OfficialUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpensMonth = table.Column<int>(type: "int", nullable: true),
                    ClosesMonth = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationGuides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityBoosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    Recipients = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityBoosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityBoosts_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunityId = table.Column<int>(type: "int", nullable: false),
                    QuestionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    QuestionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    QuestionFr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AnswerAr = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    AnswerEn = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    AnswerFr = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    Lang = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsCurated = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnsweredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityQuestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpportunityQuestions_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuccessStories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    ProgramName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Story = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Advice = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DisplayName = table.Column<int>(type: "int", nullable: false),
                    ShownName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CountryIso = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ConsentPublic = table.Column<bool>(type: "bit", nullable: false),
                    Lang = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Featured = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuccessStories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuccessStories_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmbassadorTaskSubmissions_UserId_TaskKey_Month",
                table: "AmbassadorTaskSubmissions",
                columns: new[] { "UserId", "TaskKey", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationGuides_ProgramKey",
                table: "ApplicationGuides",
                column: "ProgramKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationGuides_Slug",
                table: "ApplicationGuides",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityBoosts_OpportunityId",
                table: "OpportunityBoosts",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityQuestions_OpportunityId_Status",
                table: "OpportunityQuestions",
                columns: new[] { "OpportunityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityQuestions_UserId",
                table: "OpportunityQuestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_OpportunityId",
                table: "SuccessStories",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_Status_Featured",
                table: "SuccessStories",
                columns: new[] { "Status", "Featured" });

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_UserId",
                table: "SuccessStories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmbassadorTaskSubmissions");

            migrationBuilder.DropTable(
                name: "ApplicationGuides");

            migrationBuilder.DropTable(
                name: "OpportunityBoosts");

            migrationBuilder.DropTable(
                name: "OpportunityQuestions");

            migrationBuilder.DropTable(
                name: "SuccessStories");

            migrationBuilder.DropColumn(
                name: "IsSponsored",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "SponsoredUntil",
                table: "Opportunities");
        }
    }
}
