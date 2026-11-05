using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>فلاتر قوائم الفرص من الحقول المهيكلة (المرحلة 2.2) على SQLite بالمخطط الحقيقي والدول المبذورة.</summary>
    public class OpportunityFilterTests : IDisposable
    {
        private static readonly DateTime Today = new(2026, 11, 4);
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly List<Country> _countries;

        public OpportunityFilterTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.SaveChanges();
            _countries = _db.Countries.ToList();
            var world = _countries.First(c => c.Id == FacetParser.WorldCountryId);
            var arab = _countries.First(c => c.Id == FacetParser.ArabWorldCountryId);
            var jordan = _countries.First(c => c.IsoCode == "JO");
            var energy = _db.Fields.First(f => f.Key == "energy");

            _db.Opportunities.AddRange(
                Opp(1, OpportunityType.Scholarships, Today.AddDays(20), o => { o.AvailableCountries.Add(arab); o.FundingType = FundingType.FullyFunded; o.DegreeLevels = DegreeLevels.Master | DegreeLevels.PhD; o.Fields.Add(energy); o.GreenThemes = GreenThemes.Energy; o.HostCountryIso = "DE"; o.LanguageRequirement = LanguageRequirement.IELTS; o.PublishDate = Today.AddDays(-1); }),
                Opp(2, OpportunityType.Scholarships, Today.AddDays(60), o => { o.AvailableCountries.Add(jordan); o.FundingType = FundingType.PartiallyFunded; o.DegreeLevels = DegreeLevels.Bachelor; o.LanguageRequirement = LanguageRequirement.None; o.PublishDate = Today.AddDays(-5); }),
                Opp(3, OpportunityType.Jobs, null, o => { o.DeadlineType = DeadlineType.UntilFull; o.EligibleCountriesIso = "DE,FR"; o.IsRemote = true; o.LanguageRequirement = LanguageRequirement.Unknown; o.PublishDate = Today.AddDays(-2); }),
                Opp(4, OpportunityType.Jobs, Today.AddDays(-3), o => { o.AvailableCountries.Add(world); o.PublishDate = Today.AddDays(-10); }),  // منتهية
                Opp(5, OpportunityType.Volunteering, Today.AddDays(5), o => { o.AvailableCountries.Add(world); o.IsOnline = true; o.PublishDate = Today; }),
                Opp(6, OpportunityType.Jobs, Today.AddDays(9), o => { o.Status = OpportunityStatus.Draft; o.AvailableCountries.Add(world); }));
            _db.SaveChanges();
        }

        private static Opportunity Opp(int id, OpportunityType type, DateTime? end, Action<Opportunity>? setup = null)
        {
            var o = new Opportunity
            {
                Id = id, TitleAr = "فرصة " + id, TitleEn = "Opp " + id, TitleFr = "Opp " + id, DescriptionAr = "", DescriptionEn = "", DescriptionFr = "",
                DetailsAr = "", DetailsEn = "", DetailsFr = "", EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "",
                BenefitsAr = "", BenefitsEn = "", BenefitsFr = "", ApplyLink = "https://x.example", ImagePath = "", PublishedBy = "Admin", CreatedByUserId = "u1",
                Type = type, Status = OpportunityStatus.Published, DeadlineType = DeadlineType.SpecificDate, EndDate = end, PublishDate = Today,
                AvailableCountries = new List<Country>(), Fields = new List<Field>()
            };
            setup?.Invoke(o);
            return o;
        }

        private List<int> Ids(OpportunityQuery q)
        {
            var p = OpportunityFilter.Parse(q, _countries);
            return OpportunityFilter.Sort(OpportunityFilter.Apply(_db.Opportunities.Include(o => o.AvailableCountries), p, Today), p.Sort, Today).Select(o => o.Id).ToList();
        }

        [Fact]
        public void Default_OpenFirstThenNewest_PublishedOnly()
        {
            Assert.Equal(new[] { 5, 1, 3, 2, 4 }, Ids(new OpportunityQuery()));
        }

        [Fact]
        public void Sort_Deadline_And_Newest()
        {
            Assert.Equal(new[] { 5, 1, 2, 3, 4 }, Ids(new OpportunityQuery { Sort = "deadline" }));
            Assert.Equal(new[] { 5, 1, 3, 2, 4 }, Ids(new OpportunityQuery { Sort = "newest" }));
        }

        [Fact]
        public void Types_And_Search()
        {
            Assert.Equal(new[] { 1, 2 }, Ids(new OpportunityQuery { Type = new() { "Scholarships" } }));
            Assert.Equal(new[] { 5, 1, 3, 2, 4 }, Ids(new OpportunityQuery { Type = new() { "bogus" } })); // نوع مجهول يُهمل
        }

        [Fact]
        public void MyCountry_Iso_Name_ArabWorld_And_EligibleIsoCsv()
        {
            Assert.Equal(new[] { 5, 1, 2, 4 }, Ids(new OpportunityQuery { Country = "JO" }));       // XW + XA + JO
            Assert.Equal(new[] { 5, 1, 2, 4 }, Ids(new OpportunityQuery { Country = "الأردن" }));  // الاسم يُحلّ إلى JO
            Assert.Equal(new[] { 5, 1, 4 }, Ids(new OpportunityQuery { Country = "EG" }));         // XW + XA فقط
            Assert.Equal(new[] { 5, 3, 4 }, Ids(new OpportunityQuery { Country = "DE" }));         // XW + CSV
            Assert.Equal(new[] { 5, 1, 4 }, Ids(new OpportunityQuery { Country = "XA" }));
            Assert.Equal(new[] { 5, 4 }, Ids(new OpportunityQuery { Country = "XW" }));
        }

        [Fact]
        public void Facets_Funding_Degree_Field_Theme_Host_Remote_NoIelts_Within()
        {
            Assert.Equal(new[] { 1 }, Ids(new OpportunityQuery { Funding = "FullyFunded" }));
            Assert.Equal(new[] { 1 }, Ids(new OpportunityQuery { Degree = "Master" }));
            Assert.Equal(new[] { 2 }, Ids(new OpportunityQuery { Degree = "Bachelor" }));
            Assert.Equal(new[] { 1 }, Ids(new OpportunityQuery { Field = new() { "energy" } }));
            Assert.Equal(new[] { 1 }, Ids(new OpportunityQuery { Theme = "Energy" }));
            Assert.Equal(new[] { 1 }, Ids(new OpportunityQuery { Host = "de" }));
            Assert.Equal(new[] { 5, 3 }, Ids(new OpportunityQuery { Remote = true }));
            Assert.Equal(new[] { 2 }, Ids(new OpportunityQuery { NoIelts = true }));           // None فقط، لا Unknown
            Assert.Equal(new[] { 5 }, Ids(new OpportunityQuery { Within = 7 }));
            Assert.Equal(new[] { 5, 1 }, Ids(new OpportunityQuery { Within = 30 }));
            Assert.Equal(new[] { 5 }, Ids(new OpportunityQuery { Within = 15 }));               // 15 صارت خيارًا (4.3): #1 ينتهي بعد 20 يومًا
            Assert.Equal(new[] { 5, 1, 3, 2, 4 }, Ids(new OpportunityQuery { Within = 16 }));   // قيمة غير معروفة تُهمل
        }

        [Fact]
        public void Parse_Flags()
        {
            var one = OpportunityFilter.Parse(new OpportunityQuery { Type = new() { "jobs" } }, _countries);
            Assert.True(one.IsSingleTypeOnly);
            Assert.False(one.HasCriteriaBeyondType);
            var two = OpportunityFilter.Parse(new OpportunityQuery { Type = new() { "jobs" }, Remote = true }, _countries);
            Assert.False(two.IsSingleTypeOnly);
            Assert.True(two.HasCriteriaBeyondType);
            var sorted = OpportunityFilter.Parse(new OpportunityQuery { Sort = "newest" }, _countries);
            Assert.True(sorted.HasCriteriaBeyondType);
            Assert.Equal("open", OpportunityFilter.Parse(new OpportunityQuery { Sort = "x" }, _countries).Sort);
            Assert.Equal("Nowhere", OpportunityFilter.Parse(new OpportunityQuery { Country = "Nowhere" }, _countries).CountryName);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
