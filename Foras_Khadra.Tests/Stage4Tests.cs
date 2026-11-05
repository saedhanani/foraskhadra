using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class Stage4Tests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;

        public Stage4Tests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id, DateTime publish, bool sponsored = false, DateTime? sponsoredUntil = null, DateTime? end = null) => new()
        {
            Id = id,
            TitleAr = "t" + id, TitleEn = "t" + id, TitleFr = "t" + id,
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x", CreatedByUserId = "u1",
            Status = OpportunityStatus.Published, PublishDate = publish, IsSponsored = sponsored, SponsoredUntil = sponsoredUntil,
            DeadlineType = end.HasValue ? DeadlineType.SpecificDate : (DeadlineType?)null, EndDate = end,
            AvailableCountries = new List<Country>()
        };

        [Fact]
        public async Task SponsoredOpenOpportunityIsListedFirstInEverySort()
        {
            var today = new DateTime(2026, 11, 5);
            _db.Opportunities.AddRange(
                NewOpportunity(1, today.AddDays(-1)),
                NewOpportunity(2, today.AddDays(-10), sponsored: true),
                NewOpportunity(3, today.AddDays(-20), sponsored: true, sponsoredUntil: today.AddDays(-2)), // انتهى إبرازها
                NewOpportunity(4, today, sponsored: true, end: today.AddDays(-3)));                       // مموَّلة لكن منتهية
            await _db.SaveChangesAsync();

            foreach (var sort in OpportunityFilter.SortOptions)
            {
                var ids = await OpportunityFilter.Sort(_db.Opportunities.AsNoTracking(), sort, today).Select(o => o.Id).ToListAsync();
                Assert.Equal(2, ids[0]); // المموَّلة المفتوحة أولًا في كل ترتيب
                if (sort != "newest") Assert.Equal(4, ids[^1]); // المنتهية آخرًا رغم الإبراز (open/deadline)
            }
        }

        [Fact]
        public void WithinAcceptsFifteenDays()
        {
            Assert.Contains(15, OpportunityFilter.WithinOptions);
            var parsed = OpportunityFilter.Parse(new OpportunityQuery { Within = 15 }, new List<Country>());
            Assert.Equal(15, parsed.Within);
            Assert.Null(OpportunityFilter.Parse(new OpportunityQuery { Within = 16 }, new List<Country>()).Within);
        }

        [Fact]
        public void GuideSectionsRoundTripAndSeason()
        {
            var g = new ApplicationGuide { ProgramKey = "daad-epos", Slug = "daad-epos", OpensMonth = 8, ClosesMonth = 10 };
            g.SetSections(new GuideSections { Conditions = "أ\nب", Checklist = new() { "1", "2" } }, new GuideSections { Conditions = "a" }, new GuideSections());
            Assert.Equal(2, g.Sections("ar").Checklist.Count);
            Assert.Equal("a", g.Sections("en").Conditions);
            Assert.Equal("أ\nب", g.Sections("fr").Conditions); // الفرنسية فارغة → العربية
            Assert.Contains("August", g.Season("en"));
            Assert.Contains("أكتوبر", g.Season("ar")!);
            Assert.Equal(new[] { "x", "y" }, ApplicationGuide.ParseChecklist(" x \r\n\r\ny\n"));
        }

        [Fact]
        public void AmbassadorLevelsAndNextTarget()
        {
            Assert.Null(AmbassadorTasks.LevelFor(10, "ar"));
            Assert.Equal("bronze", AmbassadorTasks.LevelFor(20, "ar")!.Value.Key);
            Assert.Equal("silver", AmbassadorTasks.LevelFor(99, "en")!.Value.Key);
            Assert.Equal("gold", AmbassadorTasks.LevelFor(500, "fr")!.Value.Key);
            Assert.Equal(10, AmbassadorTasks.NextLevel(10, "ar")!.Value.Needed);
            Assert.Null(AmbassadorTasks.NextLevel(150, "ar"));
            Assert.Equal(AmbassadorTasks.All.Count, AmbassadorTasks.All.Select(t => t.Key).Distinct().Count());
            Assert.NotNull(AmbassadorTasks.Find(AmbassadorTasks.InviteKey));
        }

        [Fact]
        public void CalendarWithSeveralDeadlines()
        {
            var ics = CalendarHelper.BuildCalendar("مواعيدي", new[]
            {
                (1, "منحة; أ", new DateTime(2026, 12, 1), "https://www.foraskhadra.com/Opportunity/Details/1"),
                (2, "Job, B", new DateTime(2026, 12, 15), "https://www.foraskhadra.com/Opportunity/Details/2")
            }, new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc));
            Assert.Equal(2, ics.Split("BEGIN:VEVENT").Length - 1);
            Assert.Contains("X-WR-CALNAME:مواعيدي", ics);
            Assert.Contains("DTSTART;VALUE=DATE:20261201", ics);
            Assert.Contains("SUMMARY:منحة\\; أ", ics);
            Assert.Contains("SUMMARY:Job\\, B", ics);
            Assert.EndsWith("END:VCALENDAR\r\n", ics);
        }

        [Fact]
        public void SponsoredNowRespectsUntilDate()
        {
            Assert.True(new Opportunity { IsSponsored = true }.IsSponsoredNow);
            Assert.True(new Opportunity { IsSponsored = true, SponsoredUntil = DateTime.Now.AddDays(1) }.IsSponsoredNow);
            Assert.False(new Opportunity { IsSponsored = true, SponsoredUntil = DateTime.Now.AddDays(-1) }.IsSponsoredNow);
            Assert.False(new Opportunity { IsSponsored = false }.IsSponsoredNow);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
