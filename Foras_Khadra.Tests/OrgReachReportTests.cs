using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class OrgReachReportTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly OrgReachReport _report;
        private static readonly DateOnly Today = new(2026, 11, 5);

        public OrgReachReportTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _report = new OrgReachReport(_db);

            _db.Users.Add(new ApplicationUser { Id = "org1", UserName = "org1", Email = "org1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "m1", UserName = "m1", Email = "m1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "someone-else", UserName = "someone-else", Email = "else@test.local" });
            var org = new Organization
            {
                Id = 3, Name = "Org", Sector = "env", Country = "JO", ContactName = "c", ContactEmail = "c@test.local",
                PhoneNumber = "0", Password = "x", UserId = "org1", IsManual = false
            };
            _db.Organizations.Add(org);
            _db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = ""; // internal set
            _db.SaveChanges();
            _db.Opportunities.Add(NewOpportunity(1, "org1", "منحة", "Scholarship"));
            _db.Opportunities.Add(NewOpportunity(2, "org1", "تدريب", "Internship"));
            _db.Opportunities.Add(NewOpportunity(3, "someone-else", "أخرى", "Other"));
            _db.OpportunityDailyStats.AddRange(
                new OpportunityDailyStat { OpportunityId = 1, Day = Today, Views = 10, ApplyClicks = 2 },
                new OpportunityDailyStat { OpportunityId = 1, Day = Today.AddDays(-3), Views = 5, ApplyClicks = 1 },
                new OpportunityDailyStat { OpportunityId = 2, Day = Today.AddDays(-1), Views = 20, ApplyClicks = 4 },
                new OpportunityDailyStat { OpportunityId = 1, Day = Today.AddDays(-10), Views = 100, ApplyClicks = 50 }, // خارج نافذة 7 أيام
                new OpportunityDailyStat { OpportunityId = 3, Day = Today, Views = 999, ApplyClicks = 999 }); // منظمة أخرى
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "m1", OpportunityId = 2 });
            _db.OrganizationFollows.Add(new OrganizationFollow { UserId = "m1", OrganizationId = 3 });
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id, string creator, string titleAr, string titleEn) => new()
        {
            Id = id,
            TitleAr = titleAr, TitleEn = titleEn, TitleFr = titleEn,
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x",
            CreatedByUserId = creator, Status = OpportunityStatus.Published, AvailableCountries = new List<Country>()
        };

        [Fact]
        public async Task SevenDayWindowSumsOnlyTheOrganizationsRows()
        {
            var r = await _report.BuildAsync("org1", 7, "en", Today);
            Assert.NotNull(r);
            Assert.Equal("Org", r!.OrganizationName);
            Assert.Equal(7, r.Days);
            Assert.Equal(Today.AddDays(-6), r.From);
            Assert.Equal(Today, r.To);
            Assert.Equal(35, r.Views);
            Assert.Equal(7, r.Clicks);
            Assert.Equal(20, r.ClickRatePercent);
            Assert.Equal(1, r.Saves);
            Assert.Equal(1, r.Followers);
            Assert.Equal(2, r.PublishedCount);
            Assert.Equal(7, r.Series.Count);
            Assert.Equal(10, r.Series[^1].Views);

            Assert.Equal(2, r.Rows.Count);
            Assert.Equal("Internship", r.Rows[0].Title); // الأعلى مشاهدة أولًا
            Assert.Equal(20, r.Rows[0].Views);
            Assert.Equal(1, r.Rows[0].Saves);
            Assert.Equal(15, r.Rows[1].Views);
            Assert.Equal(20, r.Rows[1].ClickRatePercent);
        }

        [Fact]
        public async Task ThirtyDaysIncludesOlderRowsAndInvalidDaysFallsBackTo30()
        {
            var r = await _report.BuildAsync("org1", 12, "ar", Today);
            Assert.Equal(30, r!.Days);
            Assert.Equal(135, r.Views);
            Assert.Equal("منحة", r.Rows[0].Title);
        }

        [Fact]
        public async Task UnknownUserReturnsNull()
        {
            Assert.Null(await _report.BuildAsync("nobody", 7, "ar", Today));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
