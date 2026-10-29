using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>قاعدة SQLite في الذاكرة بالمخطط الحقيقي — نفس نمط SavedOpportunityServiceTests.</summary>
    public class NotificationSweeperTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly NotificationSweeper _sweeper;

        public NotificationSweeperTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _sweeper = new NotificationSweeper(_db);

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.SaveChanges();
        }

        private Opportunity AddOpportunity(int id, string title, OpportunityType? type = null,
            DateTime? endDate = null, DeadlineType? deadlineType = null, DateTime? publishDate = null)
        {
            var o = new Opportunity
            {
                Id = id,
                TitleAr = title, TitleEn = title, TitleFr = title,
                DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
                DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
                EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
                BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
                ApplyLink = "https://example.test",
                ImagePath = "x",
                PublishedBy = "x",
                CreatedByUserId = "u1",
                AvailableCountries = new List<Country>(),
                Status = OpportunityStatus.Published,
                Type = type,
                EndDate = endDate,
                DeadlineType = deadlineType,
                PublishDate = publishDate ?? DateTime.Now
            };
            _db.Opportunities.Add(o);
            _db.SaveChanges(); // يملأ SearchText عبر SearchIndexer
            return o;
        }

        // ===== إشعار الموعد النهائي للمحفوظات =====

        [Fact]
        public async Task SavedDeadlineSoon_CreatesOnce_AndDoesNotDuplicate()
        {
            AddOpportunity(1, "منحة قريبة", OpportunityType.Scholarships,
                endDate: DateTime.Now.Date.AddDays(3), deadlineType: DeadlineType.SpecificDate);
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "u1", OpportunityId = 1 });
            _db.SaveChanges();

            await _sweeper.SweepAsync();
            var second = await _sweeper.SweepAsync();

            Assert.Equal(0, second);
            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedDeadlineSoon));
            Assert.Equal("u1", n.UserId);
            Assert.Equal(1, n.OpportunityId);
        }

        [Fact]
        public async Task SavedDeadlineSoon_SkipsOpportunitiesAlreadyAppliedTo()
        {
            AddOpportunity(1, "منحة", OpportunityType.Scholarships,
                endDate: DateTime.Now.Date.AddDays(3), deadlineType: DeadlineType.SpecificDate);
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "u1", OpportunityId = 1, Status = ApplicationStatus.Accepted });
            _db.SaveChanges();

            await _sweeper.SweepAsync();
            Assert.Empty(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedDeadlineSoon));
            Assert.Empty(_db.Notifications.Where(x => x.Kind == NotificationKind.WeeklySummary)); // لا مواعيد تستحق الملخص
        }

        [Fact]
        public async Task SavedDeadlineSoon_IgnoresFarAndPastDeadlines()
        {
            AddOpportunity(1, "بعيدة", OpportunityType.Jobs,
                endDate: DateTime.Now.Date.AddDays(30), deadlineType: DeadlineType.SpecificDate);
            AddOpportunity(2, "منتهية", OpportunityType.Jobs,
                endDate: DateTime.Now.Date.AddDays(-1), deadlineType: DeadlineType.SpecificDate);
            _db.SavedOpportunities.AddRange(
                new SavedOpportunity { UserId = "u1", OpportunityId = 1 },
                new SavedOpportunity { UserId = "u1", OpportunityId = 2 });
            _db.SaveChanges();

            Assert.Equal(0, await _sweeper.SweepAsync());
        }

        [Fact]
        public async Task LastDay_ReplacesSevenDayReminder_ForDeadlinesTomorrow()
        {
            AddOpportunity(1, "غدًا", OpportunityType.Jobs,
                endDate: DateTime.Now.Date.AddDays(1), deadlineType: DeadlineType.SpecificDate);
            AddOpportunity(2, "بعد 3 أيام", OpportunityType.Jobs,
                endDate: DateTime.Now.Date.AddDays(3), deadlineType: DeadlineType.SpecificDate);
            _db.SavedOpportunities.AddRange(
                new SavedOpportunity { UserId = "u1", OpportunityId = 1 },
                new SavedOpportunity { UserId = "u1", OpportunityId = 2 });
            _db.SaveChanges();

            await _sweeper.SweepAsync();
            await _sweeper.SweepAsync();

            // ما ينتهي غدًا: تذكير اليوم الأخير وحده (لا تذكيران معًا)، ومرة واحدة
            Assert.Equal(1, Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedDeadlineLastDay)).OpportunityId);
            Assert.Equal(2, Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedDeadlineSoon)).OpportunityId);
        }

        [Fact]
        public async Task LastDay_SkipsOpportunitiesAlreadyAppliedTo()
        {
            AddOpportunity(1, "اليوم", OpportunityType.Jobs,
                endDate: DateTime.Now.Date, deadlineType: DeadlineType.SpecificDate);
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "u1", OpportunityId = 1, Status = ApplicationStatus.Applied });
            _db.SaveChanges();

            await _sweeper.SweepAsync();
            Assert.Empty(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedDeadlineLastDay));
        }

        // ===== إشعار الاهتمامات =====

        [Fact]
        public async Task Interests_FirstSweepSetsBaseline_ThenNotifiesNewOnly()
        {
            var user = _db.Users.Single();
            user.Interests = new List<string> { "grants" };
            _db.SaveChanges();

            int InterestNotifications() => _db.Notifications.Count(x => x.Kind == NotificationKind.InterestMatches);

            // المسح الأول: خط أساس فقط حتى لو وُجدت فرص قديمة
            AddOpportunity(1, "منحة قديمة", OpportunityType.Scholarships);
            await _sweeper.SweepAsync();
            Assert.Equal(0, InterestNotifications());
            Assert.NotNull(_db.Users.Single().LastInterestNotifiedAt);

            // نرجع خط الأساس إلى ما بعد الفرصة القديمة وقبل الجديدة → إشعار مجمّع واحد بعدد 1
            _db.Users.Single().LastInterestNotifiedAt = DateTime.Now.AddMilliseconds(-1);
            _db.SaveChanges();
            AddOpportunity(2, "منحة جديدة", OpportunityType.Scholarships);
            await _sweeper.SweepAsync();

            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.InterestMatches));
            Assert.Equal(1, n.Count);

            // لا تكرار لنفس الفرصة في المسح التالي
            await _sweeper.SweepAsync();
            Assert.Equal(1, InterestNotifications());
        }

        [Fact]
        public async Task Interests_IgnoresNonMatchingTypes()
        {
            var user = _db.Users.Single();
            user.Interests = new List<string> { "jobs" };
            _db.SaveChanges();
            await _sweeper.SweepAsync(); // خط الأساس

            AddOpportunity(1, "منحة", OpportunityType.Scholarships, publishDate: DateTime.Now.AddMinutes(1));
            Assert.Equal(0, await _sweeper.SweepAsync());
        }

        // ===== إشعار البحث المحفوظ =====

        [Fact]
        public async Task SavedSearch_NotifiesOnNewMatch_AndAdvancesLastRun()
        {
            _db.SavedSearches.Add(new SavedSearch
            {
                UserId = "u1",
                Query = "solar",
                LastRunAt = DateTime.Now.AddHours(-1)
            });
            _db.SaveChanges();

            AddOpportunity(1, "solar energy fellowship", OpportunityType.Fellowships);
            Assert.Equal(1, await _sweeper.SweepAsync());

            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.SavedSearchMatches));
            Assert.Equal(1, n.Count);
            Assert.Contains("search=solar", n.LinkParams);

            // المسح التالي لا يكرر نفس الفرصة
            Assert.Equal(0, await _sweeper.SweepAsync());
        }

        [Fact]
        public async Task SavedSearch_RespectsTypeFilter()
        {
            _db.SavedSearches.Add(new SavedSearch
            {
                UserId = "u1",
                Query = "",
                TypesCsv = "Jobs",
                LastRunAt = DateTime.Now.AddHours(-1)
            });
            _db.SaveChanges();

            AddOpportunity(1, "منحة", OpportunityType.Scholarships);
            Assert.Equal(0, await _sweeper.SweepAsync());

            AddOpportunity(2, "وظيفة", OpportunityType.Jobs);
            Assert.Equal(1, await _sweeper.SweepAsync());
        }

        // ===== مراقبة الدورة القادمة (3-3) =====

        [Fact]
        public async Task CycleWatch_NotifiesWhenSameOpportunityReopens_Once()
        {
            var old = AddOpportunity(1, "منحة DAAD للماجستير", OpportunityType.Scholarships,
                endDate: DateTime.Now.Date.AddDays(-30), deadlineType: DeadlineType.SpecificDate);
            _db.CycleWatches.Add(new CycleWatch { UserId = "u1", OpportunityId = 1 });
            _db.SaveChanges();

            Assert.Equal(0, await _sweeper.SweepAsync()); // ما زالت منتهية

            old.EndDate = DateTime.Now.Date.AddDays(60); // الإدارة مدّدت الموعد = دورة جديدة
            _db.SaveChanges();

            Assert.Equal(1, await _sweeper.SweepAsync());
            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.CycleReopened));
            Assert.Equal(1, n.OpportunityId);
            Assert.NotNull(_db.CycleWatches.Single().NotifiedAt);

            Assert.Equal(0, await _sweeper.SweepAsync()); // لا تكرار
        }

        [Fact]
        public async Task CycleWatch_NotifiesWhenSimilarTitlePublishedAfterWatch()
        {
            AddOpportunity(1, "منحة DAAD للماجستير في ألمانيا", OpportunityType.Scholarships,
                endDate: DateTime.Now.Date.AddDays(-30), deadlineType: DeadlineType.SpecificDate);
            _db.CycleWatches.Add(new CycleWatch { UserId = "u1", OpportunityId = 1, CreatedAt = DateTime.Now.AddMinutes(-5) });
            _db.SaveChanges();

            AddOpportunity(2, "وظيفة محاسب", OpportunityType.Jobs); // لا تشبه
            Assert.Equal(0, await _sweeper.SweepAsync());

            AddOpportunity(3, "منحة DAAD للماجستير في ألمانيا 2027", OpportunityType.Scholarships);
            Assert.Equal(1, await _sweeper.SweepAsync());

            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.CycleReopened));
            Assert.Equal(3, n.OpportunityId);
            Assert.Equal("منحة DAAD للماجستير في ألمانيا", n.Label);
        }

        [Fact]
        public void TitleTerms_KeepsLongestMeaningfulWords()
        {
            var terms = NotificationSweeper.TitleTerms("منحة DAAD للماجستير في ألمانيا 2027");
            Assert.True(terms.Count <= 4);
            Assert.DoesNotContain("في", terms);
            Assert.Contains("للماجستير", terms);
        }

        // ===== الاهتمام ببرنامج (5-5) =====

        private FlagshipProgram AddProgram(int id, bool open)
        {
            var p = new FlagshipProgram
            {
                Id = id, Slug = "p" + id, IsPublished = true, ApplicationsOpen = open,
                MenuTitleAr = "x", MenuTitleEn = "x", MenuTitleFr = "x",
                TitleAr = "برنامج " + id, TitleEn = "x", TitleFr = "x",
                IntroAr = "x", IntroEn = "x", IntroFr = "x"
            };
            _db.FlagshipPrograms.Add(p);
            _db.SaveChanges();
            return p;
        }

        [Fact]
        public async Task ProgramInterest_NotifiedOnceWhenApplicationsOpen()
        {
            var program = AddProgram(1, open: false);
            _db.ProgramInterests.Add(new ProgramInterest { UserId = "u1", ProgramId = 1 });
            _db.SaveChanges();

            Assert.Equal(0, await _sweeper.SweepAsync()); // مغلق

            program.ApplicationsOpen = true;
            _db.SaveChanges();
            Assert.Equal(1, await _sweeper.SweepAsync());
            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.ProgramOpened));
            Assert.Equal("p1", n.LinkParams);
            Assert.Equal("برنامج 1", n.Label);

            Assert.Equal(0, await _sweeper.SweepAsync()); // لا تكرار
        }

        // ===== ملخص الأسبوع (5-1) =====

        [Fact]
        public async Task WeeklySummary_OncePerWeek_OnlyWhenSomethingToSay()
        {
            var user = _db.Users.Single();
            user.Interests = new List<string> { "jobs" };
            user.LastInterestNotifiedAt = DateTime.Now; // حتى لا يتداخل إشعار الاهتمامات
            _db.SaveChanges();

            Assert.Equal(0, await _sweeper.SweepAsync()); // لا جديد ولا مواعيد

            AddOpportunity(1, "وظيفة جديدة", OpportunityType.Jobs);
            user.LastInterestNotifiedAt = DateTime.Now; _db.SaveChanges();
            Assert.Equal(1, await _sweeper.SweepAsync());
            var n = Assert.Single(_db.Notifications.Where(x => x.Kind == NotificationKind.WeeklySummary));
            Assert.Equal(1, n.Count);

            // خلال الأسبوع نفسه: لا ملخص ثانٍ
            user.LastInterestNotifiedAt = DateTime.Now; _db.SaveChanges();
            Assert.Equal(0, await _sweeper.SweepAsync());

            // بعد أسبوع: ملخص جديد
            n.CreatedAt = DateTime.Now.AddDays(-8); _db.SaveChanges();
            Assert.Equal(1, await _sweeper.SweepAsync());
        }

        // ===== الدوال المساعدة =====

        [Fact]
        public void ParseTypes_IgnoresUnknownAndDuplicates()
        {
            var types = NotificationSweeper.ParseTypes("Jobs, Jobs, Nope, Scholarships");
            Assert.Equal(new[] { OpportunityType.Jobs, OpportunityType.Scholarships }, types);
            Assert.Empty(NotificationSweeper.ParseTypes(null));
        }

        [Fact]
        public void BuildLinkParams_EncodesQueryAndJoinsParts()
        {
            var s = new SavedSearch { Query = "طاقة شمسية", TypesCsv = "Jobs", Country = "مصر" };
            var p = NotificationSweeper.BuildLinkParams(s);
            Assert.Contains("search=", p);
            Assert.Contains("type=Jobs", p);
            Assert.Contains("country=", p);
            Assert.DoesNotContain(" ", p);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
