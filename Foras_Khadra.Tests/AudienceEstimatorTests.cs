using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>تقدير الأعضاء المؤهَّلين من بيانات التسجيل (الاهتمامات + الجنسية/الإقامة) — SQLite في الذاكرة.</summary>
    public class AudienceEstimatorTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly AudienceEstimator _estimator;

        public AudienceEstimatorTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _estimator = new AudienceEstimator(_db, new MemoryCache(new MemoryCacheOptions()));

            // الدول من بذرة ApplicationDbContext (XW، XA، و22 دولة عربية برموز ISO)؛ "Germany" ليست فيها فتُقرأ كبلد مجهول
            // ثلاثة أعضاء وحساب منظمة واحد (لا يُحسب)
            _db.Users.AddRange(
                User("u1", UserRole.User, new[] { "jobs", "grants" }, nationality: "Jordan", country: "الأردن"),
                User("u2", UserRole.User, new[] { "grants" }, nationality: "مصر", country: "Germany"),
                User("u3", UserRole.User, Array.Empty<string>(), nationality: "Germany", country: "Germany"),
                User("org", UserRole.Organization, new[] { "jobs" }, nationality: "Jordan", country: "Jordan"));
            _db.SaveChanges();
        }

        private static ApplicationUser User(string id, UserRole role, string[] interests, string nationality, string country) =>
            new() { Id = id, UserName = id, Email = id + "@test.local", Role = role, Interests = interests.ToList(), Nationality = nationality, Country = country };

        [Fact]
        public async Task NoFilter_CountsAllMembersOnly()
        {
            Assert.Equal(3, await _estimator.EstimateAsync(null, null));
            Assert.Equal(3, await _estimator.EstimateAsync(null, "XW"));
        }

        [Fact]
        public async Task TypeFilter_UsesInterests()
        {
            Assert.Equal(2, await _estimator.EstimateAsync(OpportunityType.Scholarships, null));
            Assert.Equal(1, await _estimator.EstimateAsync(OpportunityType.Jobs, null));
            Assert.Equal(0, await _estimator.EstimateAsync(OpportunityType.Conferences, null));
        }

        [Fact]
        public async Task CountryFilter_MatchesNationalityOrResidence_InAnyLanguage()
        {
            Assert.Equal(1, await _estimator.EstimateAsync(null, "JO"));   // u1 (الأردن / Jordan)
            Assert.Equal(1, await _estimator.EstimateAsync(null, "EG"));   // u2 بالجنسية
            Assert.Equal(0, await _estimator.EstimateAsync(null, "DE"));   // بلد غير معروف في الجدول → لا أحد
            Assert.Equal(2, await _estimator.EstimateAsync(null, "XA"));   // u1 + u2
            Assert.Equal(1, await _estimator.EstimateAsync(OpportunityType.Scholarships, "EG"));
        }

        [Fact]
        public async Task Numbers_CountMembersOpenOpportunitiesAndCountries()
        {
            _db.Opportunities.AddRange(
                Opp(1, OpportunityStatus.Published, DateTime.Today.AddDays(5)),
                Opp(2, OpportunityStatus.Published, DateTime.Today.AddDays(-1)),   // منتهية
                Opp(3, OpportunityStatus.PendingReview, DateTime.Today.AddDays(5)));
            _db.SaveChanges();

            var n = await _estimator.NumbersAsync();
            Assert.Equal(3, n.Members);
            Assert.Equal(1, n.OpenOpportunities);
            Assert.Equal(2, n.MemberCountries); // JO, EG (ألمانيا غير معروفة)
        }

        private static Opportunity Opp(int id, OpportunityStatus status, DateTime end) => new()
        {
            Id = id, TitleAr = "ع", TitleEn = "e", TitleFr = "f", DescriptionAr = "", DescriptionEn = "", DescriptionFr = "",
            DetailsAr = "", DetailsEn = "", DetailsFr = "", EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "",
            BenefitsAr = "", BenefitsEn = "", BenefitsFr = "", ApplyLink = "https://x.example", ImagePath = "", PublishedBy = "Admin",
            CreatedByUserId = "u1", Status = status, DeadlineType = DeadlineType.SpecificDate, EndDate = end, PublishDate = DateTime.Now
        };

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
