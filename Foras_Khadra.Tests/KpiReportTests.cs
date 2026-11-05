using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>لوحة مؤشرات المدير (5.1): كل رقم يُحسب من جداوله على SQLite، مع أهداف عمليات المحتوى.</summary>
    public class KpiReportTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;

        private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0);
        private static readonly DateOnly To = DateOnly.FromDateTime(Now);
        private static readonly DateOnly From = To.AddDays(-6);

        public KpiReportTests()
        {
            _connection.Open();
            _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
        }

        private static DateTime Ago(double days) => Now.AddDays(-days);
        private static DateTime HoursAgo(double hours) => Now.AddHours(-hours);

        private static Opportunity Opp(string title, OpportunityStatus status, DateTime publish, DateTime? submitted = null, DateTime? reviewed = null) => new()
        {
            TitleAr = title, TitleEn = title, TitleFr = title,
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x", CreatedByUserId = "u1",
            Status = status, PublishDate = publish, SubmittedAt = submitted, ReviewedAt = reviewed,
            AvailableCountries = new List<Country>()
        };

        private void SeedEverything()
        {
            _db.Users.AddRange(
                new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local", Role = UserRole.User, EmailConfirmed = true, CreatedAt = Ago(1) },
                new ApplicationUser { Id = "u2", UserName = "u2", Email = "u2@test.local", Role = UserRole.User, EmailConfirmed = false, CreatedAt = Ago(2) },
                new ApplicationUser { Id = "u3", UserName = "u3", Email = "u3@test.local", Role = UserRole.User, EmailConfirmed = true, CreatedAt = Ago(10) }, // المدة السابقة
                new ApplicationUser { Id = "o1", UserName = "o1", Email = "o1@test.local", Role = UserRole.Organization, CreatedAt = Ago(1) },
                new ApplicationUser { Id = "a1", UserName = "a1", Email = "a1@test.local", Role = UserRole.Admin, CreatedAt = Ago(1) });

            _db.EligibilityProfiles.AddRange(
                new EligibilityProfile { UserId = "u1", CreatedAt = Ago(1), UpdatedAt = Ago(1) },
                new EligibilityProfile { UserId = "u3", CreatedAt = Ago(20), UpdatedAt = Ago(20) });

            _db.SiteEventDailyStats.AddRange(
                new SiteEventDailyStat { Day = To.AddDays(-1), Event = "compass.start", Count = 10 },
                new SiteEventDailyStat { Day = To.AddDays(-1), Event = "compass.result", Count = 4 },
                new SiteEventDailyStat { Day = To.AddDays(-1), Event = "compass.start", Label = "x", Count = 5 }, // بتسمية: لا يُحسب
                new SiteEventDailyStat { Day = To.AddDays(-9), Event = "compass.start", Count = 99 });            // خارج المدة

            _db.PageViewDailyStats.AddRange(
                new PageViewDailyStat { Day = To, Path = "/for-organizations", Culture = "ar", Views = 30 },
                new PageViewDailyStat { Day = To.AddDays(-2), Path = "/for-organizations", Culture = "en", Views = 20 },
                new PageViewDailyStat { Day = To, Path = "/", Culture = "ar", Views = 500 });

            _db.PublishRequests.AddRange(
                new PublishRequest { OrganizationName = "A", ContactEmail = "a@t.local", SourceUrl = "https://a.test", CreatedAt = Ago(3), Status = PublishRequestStatus.Published, ClosedAt = Ago(3).AddHours(6) },
                new PublishRequest { OrganizationName = "B", ContactEmail = "b@t.local", SourceUrl = "https://b.test", CreatedAt = Ago(2), Status = PublishRequestStatus.Rejected, ClosedAt = Ago(2).AddHours(30) },
                new PublishRequest { OrganizationName = "C", ContactEmail = "c@t.local", SourceUrl = "https://c.test", CreatedAt = Ago(1), Status = PublishRequestStatus.New },
                new PublishRequest { OrganizationName = "D", ContactEmail = "d@t.local", SourceUrl = "https://d.test", CreatedAt = Ago(12), Status = PublishRequestStatus.Published, ClosedAt = Ago(11) });

            foreach (var (name, created) in new[] { ("Org new", Ago(1)), ("Org old", Ago(30)) })
            {
                var org = new Organization { Name = name, Sector = "s", Country = "JO", ContactName = "c", ContactEmail = name + "@t.local", PhoneNumber = "0", Password = "x", CreatedAt = created };
                _db.Organizations.Add(org);
                _db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = "";
            }

            _db.Opportunities.AddRange(
                Opp("p1", OpportunityStatus.Published, Ago(0.2)),
                Opp("p2", OpportunityStatus.Published, Ago(1.2)),
                Opp("p3", OpportunityStatus.Published, Ago(1.5)),
                Opp("old", OpportunityStatus.Published, Ago(9)),
                Opp("pending", OpportunityStatus.PendingReview, Ago(2), submitted: HoursAgo(30)),
                Opp("rejected", OpportunityStatus.Rejected, Ago(5), submitted: HoursAgo(50), reviewed: HoursAgo(40)),           // 10 ساعات
                Opp("rejected-legacy", OpportunityStatus.Rejected, Ago(3), reviewed: Ago(2)),                                     // بلا SubmittedAt: من تاريخ النشر = 24 ساعة
                Opp("approved", OpportunityStatus.Published, Ago(0.1), reviewed: Ago(0.1)),                                      // اليوم؛ مراجعة بلا زمن مقيس
                Opp("draft", OpportunityStatus.Draft, Ago(1), submitted: Ago(1)));

            _db.Articles.AddRange(
                new Article { TitleAr = "a", TitleEn = "a", TitleFr = "a", ContentAr = "x", ContentEn = "x", ContentFr = "x", ImagePath = "x", Author = "x", PublishDate = Ago(1) },
                new Article { TitleAr = "b", TitleEn = "b", TitleFr = "b", ContentAr = "x", ContentEn = "x", ContentFr = "x", ImagePath = "x", Author = "x", PublishDate = Ago(20) });

            _db.ApplicationGuides.AddRange(
                new ApplicationGuide { ProgramKey = "g1", Slug = "g1", TitleAr = "g", TitleEn = "g", TitleFr = "g", SectionsJson = "{}", IsPublished = true, CreatedAt = Ago(1), UpdatedAt = Ago(1) },
                new ApplicationGuide { ProgramKey = "g2", Slug = "g2", TitleAr = "g", TitleEn = "g", TitleFr = "g", SectionsJson = "{}", IsPublished = false, CreatedAt = Ago(1), UpdatedAt = Ago(1) },
                new ApplicationGuide { ProgramKey = "g3", Slug = "g3", TitleAr = "g", TitleEn = "g", TitleFr = "g", SectionsJson = "{}", IsPublished = true, CreatedAt = Ago(20), UpdatedAt = Ago(20) });

            _db.SaveChanges();
        }

        [Fact]
        public async Task BuildsEveryIndicatorForThePeriod()
        {
            SeedEverything();

            var k = await new KpiReport(_db).BuildAsync(From, To, Now);

            Assert.Equal(7, k.Days);
            Assert.Equal(2, k.NewUsers);
            Assert.Equal(1, k.NewUsersConfirmed);
            Assert.Equal(1, k.NewUsersPrevious);

            Assert.Equal(1, k.CompassProfiles);
            Assert.Equal(10, k.CompassStarts);
            Assert.Equal(4, k.CompassResults);

            Assert.Equal(50, k.GatewayViews);
            Assert.Equal(3, k.PublishRequests);
            Assert.Equal(1, k.PublishRequestsPublished);
            Assert.Equal(1, k.PublishRequestsRejected);
            Assert.Equal(18, k.PublishRequestMedianHours!.Value, 3); // الوسيط بين 6 و30
            Assert.Equal(1, k.NewOrganizations);

            Assert.Equal(4, k.OpportunitiesPublished); // p1 + p2 + p3 + approved
            Assert.Equal(7, k.OpportunitiesPerDay.Count);
            Assert.Equal(2, k.OpportunitiesPerDay[^1].Count); // اليوم: p1 + approved
            Assert.Equal(2, k.OpportunitiesPerDay[^2].Count); // أمس: p2 + p3
            Assert.Equal(4 / 7.0, k.OpportunitiesPerDayAverage, 6);
            Assert.False(k.MeetsOpportunitiesTarget);

            Assert.Equal(3, k.Reviewed);
            Assert.Equal(17, k.ReviewMedianHours!.Value, 3); // الوسيط بين 10 و24
            Assert.True(k.MeetsReviewTarget);
            Assert.Equal(1, k.Pending);
            Assert.Equal(30, k.OldestPendingHours!.Value, 3);
            Assert.Equal(1, k.Drafts);

            Assert.Equal(1, k.ArticlesPublished);
            Assert.Equal(1, k.GuidesAdded);
            Assert.Equal(2, k.ContentPerWeek, 6);
            Assert.True(k.MeetsContentTarget);
        }

        [Fact]
        public async Task EmptyDatabaseGivesZerosAndNoTargetsJudgedOnMissingData()
        {
            var k = await new KpiReport(_db).BuildAsync(From, To, Now);

            Assert.Equal(0, k.NewUsers);
            Assert.Equal(0, k.OpportunitiesPublished);
            Assert.All(k.OpportunitiesPerDay, d => Assert.Equal(0, d.Count));
            Assert.Null(k.ReviewMedianHours);
            Assert.Null(k.MeetsReviewTarget);
            Assert.Null(k.OldestPendingHours);
            Assert.Null(k.PublishRequestMedianHours);
            Assert.False(k.MeetsOpportunitiesTarget);
            Assert.False(k.MeetsContentTarget);
        }

        [Fact]
        public void MedianHandlesOddEvenAndEmptyLists()
        {
            Assert.Null(KpiReport.Median(Array.Empty<double>()));
            Assert.Equal(2, KpiReport.Median(new double[] { 3, 1, 2 }));
            Assert.Equal(2.5, KpiReport.Median(new double[] { 4, 1, 3, 2 }));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
