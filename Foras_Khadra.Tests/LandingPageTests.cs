using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>صفحات النية (المرحلة 2.3): دمج الفلتر، الأسئلة الشائعة، شرط الفهرسة، والدفعة الأولى.</summary>
    public class LandingPageTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;

        public LandingPageTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
        }

        [Fact]
        public void Merge_RequestOverridesBase_ButEmptyRequestKeepsBase()
        {
            var baseQ = new OpportunityQuery { Country = "EG", Type = new() { "Scholarships" }, Funding = "FullyFunded", Remote = true };
            var merged = OpportunityQuery.Merge(baseQ, new OpportunityQuery { Degree = "Master", Sort = "deadline", Page = 3 });
            Assert.Equal("EG", merged.Country);
            Assert.Equal(new[] { "Scholarships" }, merged.Type);
            Assert.Equal("FullyFunded", merged.Funding);
            Assert.True(merged.Remote);
            Assert.Equal("Master", merged.Degree);
            Assert.Equal("deadline", merged.Sort);
            Assert.Equal(3, merged.Page);

            var overridden = OpportunityQuery.Merge(baseQ, new OpportunityQuery { Country = "JO", Type = new() { "Jobs" } });
            Assert.Equal("JO", overridden.Country);
            Assert.Equal(new[] { "Jobs" }, overridden.Type);
        }

        [Fact]
        public void Faq_TextRoundTrip_AndJson()
        {
            var items = LandingPage.ParseFaqText("س1؟\nج1\n\nس2؟\nسطر أول\nسطر ثانٍ\n\nبلا جواب");
            Assert.Equal(2, items.Count);
            Assert.Equal("س2؟", items[1].Q);
            Assert.Equal("سطر أول سطر ثانٍ", items[1].A);
            Assert.Equal("س1؟\nج1\n\nس2؟\nسطر أول سطر ثانٍ", LandingPage.FaqToText(items));

            var page = new LandingPage();
            page.SetFaq(items, new() { new FaqItem("Q?", "A.") }, new());
            Assert.Equal(2, page.Faq("ar").Count);
            Assert.Single(page.Faq("en"));
            Assert.Empty(page.Faq("fr"));
        }

        [Fact]
        public void Query_RoundTrip_DropsPageAndSort()
        {
            var page = new LandingPage();
            page.SetQuery(new OpportunityQuery { Country = "XA", Women = true, Within = 30, Field = new() { "energy" }, Page = 7, Sort = "newest" });
            var q = page.Query();
            Assert.Equal("XA", q.Country);
            Assert.True(q.Women);
            Assert.Equal(30, q.Within);
            Assert.Equal(new[] { "energy" }, q.Field);
            Assert.Equal(1, q.Page);
            Assert.Null(q.Sort);
            Assert.DoesNotContain("page", page.FilterJson);
            Assert.Equal(new OpportunityQuery().Country, new LandingPage { FilterJson = "not json" }.Query().Country); // تالف → فارغ
        }

        [Fact]
        public void Indexable_NeedsPublishedIntroAndFiveOpen()
        {
            var words = string.Join(" ", Enumerable.Repeat("كلمة", LandingPage.MinIntroWords));
            var page = new LandingPage { IsPublished = true, IntroAr = words, IntroEn = "short" };
            Assert.True(page.IsIndexable(5, "ar"));
            Assert.False(page.IsIndexable(4, "ar"));
            Assert.False(page.IsIndexable(50, "en"));   // مقدمة قصيرة بهذه اللغة
            page.IsPublished = false;
            Assert.False(page.IsIndexable(50, "ar"));
        }

        [Fact]
        public void Seed_FirstBatch_IsCompleteAndValid()
        {
            var countries = _db.Countries.ToList();
            var pages = LandingPageSeed.All(countries);
            Assert.True(pages.Count >= 36, $"only {pages.Count}");
            Assert.Equal(pages.Count, pages.Select(p => p.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (var p in pages)
            {
                Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", p.Slug);
                Assert.Null(OpportunityTypePages.BySlug(p.Slug));
                foreach (var lang in new[] { "ar", "en", "fr" })
                {
                    Assert.False(string.IsNullOrWhiteSpace(p.Title(lang)), p.Slug);
                    Assert.True(p.HasIntro(lang), $"{p.Slug}/{lang} intro too short");
                    Assert.Equal(3, p.Faq(lang).Count);
                }
                var q = p.Query();
                Assert.True(q.Type.Count > 0 || q.Country != null || q.Funding != null || q.NoIelts || q.Women || q.Remote || q.Host != null, p.Slug + " has no filter");
            }
            Assert.Contains(pages, p => p.Slug == "jordan" && p.Query().Country == "JO");
            Assert.Contains(pages, p => p.Slug == "scholarships-in-germany" && p.Query().Host == "DE");
            Assert.Contains(pages, p => p.Slug == "no-ielts" && p.Query().NoIelts);
        }

        [Fact]
        public void Form_RoundTrip()
        {
            var page = LandingPageSeed.All(_db.Countries.ToList()).First(p => p.Slug == "fully-funded");
            var form = LandingPageForm.From(page);
            Assert.Equal("FullyFunded", form.Funding);
            Assert.Contains("\n\n", form.FaqAr);
            form.Slug = "Fully-Funded";
            form.Women = true;
            var target = new LandingPage();
            form.ApplyTo(target);
            Assert.Equal("fully-funded", target.Slug);
            Assert.True(target.Query().Women);
            Assert.Equal("FullyFunded", target.Query().Funding);
            Assert.Equal(3, target.Faq("en").Count);
        }

        [Fact]
        public async Task OpenCount_UsesTheSavedFilter()
        {
            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            var jordan = _db.Countries.First(c => c.IsoCode == "JO");
            var today = DateTime.Today;
            for (var i = 1; i <= 6; i++)
            {
                var o = new Opportunity
                {
                    Id = i, TitleAr = "ع", TitleEn = "e", TitleFr = "f", DescriptionAr = "", DescriptionEn = "", DescriptionFr = "", DetailsAr = "", DetailsEn = "", DetailsFr = "",
                    EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "", BenefitsAr = "", BenefitsEn = "", BenefitsFr = "", ApplyLink = "https://x.example",
                    ImagePath = "", PublishedBy = "Admin", CreatedByUserId = "u1", Type = OpportunityType.Jobs, Status = OpportunityStatus.Published,
                    DeadlineType = DeadlineType.SpecificDate, EndDate = i == 6 ? today.AddDays(-1) : today.AddDays(10), PublishDate = today,
                    AvailableCountries = new List<Country> { jordan }
                };
                _db.Opportunities.Add(o);
            }
            _db.SaveChanges();
            var page = new LandingPage();
            page.SetQuery(new OpportunityQuery { Country = "JO" });
            Assert.Equal(5, await LandingPage.OpenCountAsync(_db, page, _db.Countries.ToList(), today)); // المنتهية لا تُعدّ
            page.SetQuery(new OpportunityQuery { Country = "EG" });
            Assert.Equal(0, await LandingPage.OpenCountAsync(_db, page, _db.Countries.ToList(), today));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
