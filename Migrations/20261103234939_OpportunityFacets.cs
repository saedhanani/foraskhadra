using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Foras_Khadra.Migrations
{
    /// <inheritdoc />
    public partial class OpportunityFacets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BenefitsSummaryJson",
                table: "Opportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DegreeLevels",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EligibleCountriesIso",
                table: "Opportunities",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ForWomenOnly",
                table: "Opportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FundingType",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GreenThemes",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasApplicationFee",
                table: "Opportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HostCountryIso",
                table: "Opportunities",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostOrganizationLogoUrl",
                table: "Opportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostOrganizationName",
                table: "Opportunities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostOrganizationUrl",
                table: "Opportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Opportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "Opportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LanguageRequirement",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LanguageScore",
                table: "Opportunities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAt",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "Opportunities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "Opportunities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramKey",
                table: "Opportunities",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Venue",
                table: "Opportunities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IsoCode",
                table: "Countries",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityFields",
                columns: table => new
                {
                    FieldsId = table.Column<int>(type: "int", nullable: false),
                    OpportunitiesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityFields", x => new { x.FieldsId, x.OpportunitiesId });
                    table.ForeignKey(
                        name: "FK_OpportunityFields_Fields_FieldsId",
                        column: x => x.FieldsId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpportunityFields_Opportunities_OpportunitiesId",
                        column: x => x.OpportunitiesId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsoCode",
                value: "XW");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsoCode",
                value: "XA");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsoCode",
                value: "PS");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsoCode",
                value: "JO");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsoCode",
                value: "SY");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsoCode",
                value: "LB");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsoCode",
                value: "IQ");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsoCode",
                value: "SA");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsoCode",
                value: "KW");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 10,
                column: "IsoCode",
                value: "QA");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 11,
                column: "IsoCode",
                value: "BH");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 12,
                column: "IsoCode",
                value: "AE");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 13,
                column: "IsoCode",
                value: "OM");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 14,
                column: "IsoCode",
                value: "YE");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 15,
                column: "IsoCode",
                value: "EG");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 16,
                column: "IsoCode",
                value: "SD");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 17,
                column: "IsoCode",
                value: "LY");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 18,
                column: "IsoCode",
                value: "TN");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 19,
                column: "IsoCode",
                value: "DZ");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 20,
                column: "IsoCode",
                value: "MA");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 21,
                column: "IsoCode",
                value: "MR");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 22,
                column: "IsoCode",
                value: "DJ");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 23,
                column: "IsoCode",
                value: "SO");

            migrationBuilder.UpdateData(
                table: "Countries",
                keyColumn: "Id",
                keyValue: 24,
                column: "IsoCode",
                value: "KM");

            migrationBuilder.InsertData(
                table: "Fields",
                columns: new[] { "Id", "Key", "NameAr", "NameEn", "NameFr", "SortOrder" },
                values: new object[,]
                {
                    { 1, "environment", "البيئة والمناخ", "Environment & Climate", "Environnement et climat", 1 },
                    { 2, "energy", "الطاقة", "Energy", "Énergie", 2 },
                    { 3, "water", "المياه", "Water", "Eau", 3 },
                    { 4, "agriculture", "الزراعة والغذاء", "Agriculture & Food", "Agriculture et alimentation", 4 },
                    { 5, "engineering", "الهندسة", "Engineering", "Ingénierie", 5 },
                    { 6, "architecture", "العمارة والتخطيط العمراني", "Architecture & Urban Planning", "Architecture et urbanisme", 6 },
                    { 7, "it", "تقنية المعلومات والبرمجة", "Computing & IT", "Informatique", 7 },
                    { 8, "data-ai", "البيانات والذكاء الاصطناعي", "Data & AI", "Données et IA", 8 },
                    { 9, "natural-sciences", "العلوم الطبيعية", "Natural Sciences", "Sciences naturelles", 9 },
                    { 10, "health", "الصحة والطب", "Health & Medicine", "Santé et médecine", 10 },
                    { 11, "business", "الأعمال والإدارة", "Business & Management", "Commerce et gestion", 11 },
                    { 12, "economics", "الاقتصاد والتمويل", "Economics & Finance", "Économie et finance", 12 },
                    { 13, "entrepreneurship", "ريادة الأعمال والابتكار", "Entrepreneurship & Innovation", "Entrepreneuriat et innovation", 13 },
                    { 14, "law", "القانون", "Law", "Droit", 14 },
                    { 15, "political-science", "العلوم السياسية والعلاقات الدولية", "Political Science & International Relations", "Sciences politiques et relations internationales", 15 },
                    { 16, "development", "التنمية والعمل الإنساني", "Development & Humanitarian Work", "Développement et action humanitaire", 16 },
                    { 17, "leadership", "القيادة والمجتمع المدني", "Leadership & Civil Society", "Leadership et société civile", 17 },
                    { 18, "education", "التعليم", "Education", "Éducation", 18 },
                    { 19, "social-sciences", "العلوم الاجتماعية", "Social Sciences", "Sciences sociales", 19 },
                    { 20, "languages", "اللغات والأدب", "Languages & Literature", "Langues et littérature", 20 },
                    { 21, "arts", "الفنون والتصميم", "Arts & Design", "Arts et design", 21 },
                    { 22, "media", "الإعلام والاتصال", "Media & Communication", "Médias et communication", 22 },
                    { 23, "tourism", "السياحة والضيافة", "Tourism & Hospitality", "Tourisme et hôtellerie", 23 },
                    { 24, "vocational", "المهن والحرف", "Vocational & Trades", "Métiers et formation professionnelle", 24 },
                    { 25, "any", "كل المجالات", "All fields", "Tous les domaines", 25 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_ProgramKey",
                table: "Opportunities",
                column: "ProgramKey");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_Key",
                table: "Fields",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityFields_OpportunitiesId",
                table: "OpportunityFields",
                column: "OpportunitiesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityFields");

            migrationBuilder.DropTable(
                name: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_ProgramKey",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "BenefitsSummaryJson",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "DegreeLevels",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "EligibleCountriesIso",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ForWomenOnly",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "FundingType",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "GreenThemes",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "HasApplicationFee",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "HostCountryIso",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "HostOrganizationLogoUrl",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "HostOrganizationName",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "HostOrganizationUrl",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "LanguageRequirement",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "LanguageScore",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "LastCheckedAt",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ProgramKey",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Venue",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "IsoCode",
                table: "Countries");
        }
    }
}
