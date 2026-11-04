using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>عدّادات الذاكرة → صفوف يومية: التجميع، الزيادة على صف موجود، تجاهل التنقل الداخلي والفرص المحذوفة.</summary>
    public class AnalyticsFlushTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;

        public AnalyticsFlushTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.Opportunities.Add(new Opportunity
            {
                Id = 7, TitleAr = "ع", TitleEn = "e", TitleFr = "f", DescriptionAr = "", DescriptionEn = "", DescriptionFr = "",
                DetailsAr = "", DetailsEn = "", DetailsFr = "", EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "",
                BenefitsAr = "", BenefitsEn = "", BenefitsFr = "", ApplyLink = "https://x.example", ImagePath = "", PublishedBy = "Admin",
                CreatedByUserId = "u1", Status = OpportunityStatus.Published, PublishDate = DateTime.Now
            });
            _db.SaveChanges();
        }

        [Fact]
        public void Buffer_AggregatesAndDrains()
        {
            var a = new Analytics();
            a.PageView("/", "ar", "direct", null);
            a.PageView("/", "ar", "google.com", null);
            a.PageView("/", "en", "internal", null);
            a.PageView("/opportunity/details/7", "ar", "internal", 7);
            a.PageView("/opportunity/details/7", "ar", "direct", 7);
            a.Event("search.zero", "  Data   Science ");
            a.Event("search.zero", "data science");
            a.Event("register.done");

            var snap = a.Drain();
            var today = DateOnly.FromDateTime(DateTime.Now);
            Assert.Equal(2, snap.Views[(today, "/", "ar")]);
            Assert.Equal(1, snap.Views[(today, "/", "en")]);
            Assert.Equal(2, snap.Sources[(today, "direct")]);
            Assert.Equal(1, snap.Sources[(today, "google.com")]);
            Assert.False(snap.Sources.ContainsKey((today, "internal"))); // التنقل الداخلي لا يُعدّ مصدرًا
            Assert.Equal(2, snap.OpportunityViews[(today, 7)]);
            Assert.Equal(2, snap.Events[(today, "search.zero", "data science")]);
            Assert.Equal(1, snap.Events[(today, "register.done", null)]);
            Assert.True(a.Drain().IsEmpty); // أُفرغت
        }

        [Fact]
        public async Task Flush_CreatesThenIncrementsRows_AndSkipsDeletedOpportunities()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var snap = new AnalyticsSnapshot(
                new() { [(today, "/", "ar")] = 3, [(today, "/home/aboutus", "fr")] = 1 },
                new() { [(today, "direct")] = 2, [(today, "google.com")] = 1 },
                new() { [(today, "search.zero", "منح")] = 2, [(today, "register.done", null)] = 1 },
                new() { [(today, 7)] = 4, [(today, 999)] = 1 });

            await AnalyticsFlusher.FlushAsync(_db, snap);
            await AnalyticsFlusher.FlushAsync(_db, snap); // الكتابة الثانية تزيد على الصفوف نفسها

            Assert.Equal(6, _db.PageViewDailyStats.Single(r => r.Path == "/" && r.Culture == "ar").Views);
            Assert.Equal(2, _db.PageViewDailyStats.Single(r => r.Path == "/home/aboutus").Views);
            Assert.Equal(2, _db.PageViewDailyStats.Count());
            Assert.Equal(4, _db.TrafficSourceDailyStats.Single(r => r.Source == "direct").Views);
            Assert.Equal(4, _db.SiteEventDailyStats.Single(r => r.Event == "search.zero" && r.Label == "منح").Count);
            Assert.Equal(2, _db.SiteEventDailyStats.Single(r => r.Event == "register.done" && r.Label == null).Count);
            Assert.Equal(8, _db.OpportunityDailyStats.Single(r => r.OpportunityId == 7).Views);
            Assert.False(_db.OpportunityDailyStats.Any(r => r.OpportunityId == 999));
        }

        [Fact]
        public async Task Flush_EmptySnapshot_WritesNothing()
        {
            await AnalyticsFlusher.FlushAsync(_db, new Analytics().Drain());
            Assert.Equal(0, _db.PageViewDailyStats.Count());
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
