using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class ApplyClickCounterTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly ApplyClickCounter _counter;

        public ApplyClickCounterTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _counter = new ApplyClickCounter(_db);

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.Opportunities.Add(new Opportunity
            {
                Id = 1,
                TitleAr = "t", TitleEn = "t", TitleFr = "t",
                DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
                DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
                EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
                BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
                ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x",
                CreatedByUserId = "u1", AvailableCountries = new List<Country>()
            });
            _db.SaveChanges();
        }

        [Fact]
        public async Task CountsIntoOneRowPerDay()
        {
            var day = new DateOnly(2026, 11, 4);
            await _counter.CountAsync(1, day);
            await _counter.CountAsync(1, day);
            await _counter.CountAsync(1, day);
            await _counter.CountAsync(1, day.AddDays(1));

            var rows = _db.OpportunityDailyStats.Where(s => s.OpportunityId == 1).OrderBy(s => s.Day).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(3, rows[0].ApplyClicks);
            Assert.Equal(1, rows[1].ApplyClicks);
            Assert.All(rows, r => Assert.Equal(0, r.Views));
        }

        [Fact]
        public async Task DefaultsToToday()
        {
            await _counter.CountAsync(1);
            var row = _db.OpportunityDailyStats.Single();
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now), row.Day);
            Assert.Equal(1, row.ApplyClicks);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
