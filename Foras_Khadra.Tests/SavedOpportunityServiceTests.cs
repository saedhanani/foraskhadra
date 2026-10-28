using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>قاعدة SQLite في الذاكرة بالمخطط الحقيقي (الفهرس الفريد والحذف المتسلسل يعملان).</summary>
    public class SavedOpportunityServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly SavedOpportunityService _service;

        public SavedOpportunityServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _service = new SavedOpportunityService(_db);

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "u2", UserName = "u2", Email = "u2@test.local" });
            _db.Opportunities.Add(NewOpportunity(1, OpportunityStatus.Published));
            _db.Opportunities.Add(NewOpportunity(2, OpportunityStatus.Published));
            _db.Opportunities.Add(NewOpportunity(3, OpportunityStatus.Draft));
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id, OpportunityStatus status) => new()
        {
            Id = id,
            TitleAr = "x", TitleEn = "x", TitleFr = "x",
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", PublishedBy = "admin", ImagePath = "", CreatedByUserId = "u1",
            AvailableCountries = new List<Country>(),
            Status = status
        };

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Toggle_SavesThenUnsaves()
        {
            Assert.True(await _service.ToggleAsync("u1", 1));
            Assert.True(await _service.IsSavedAsync("u1", 1));

            Assert.False(await _service.ToggleAsync("u1", 1));
            Assert.False(await _service.IsSavedAsync("u1", 1));
        }

        [Fact]
        public async Task Toggle_IgnoresDraftsAndUnknownOpportunities()
        {
            Assert.False(await _service.ToggleAsync("u1", 3));
            Assert.False(await _service.ToggleAsync("u1", 999));
            Assert.Empty(_db.SavedOpportunities);
        }

        [Fact]
        public async Task SavedIds_ReturnsOnlyThisUsersSavedOnesAmongTheGivenIds()
        {
            await _service.ToggleAsync("u1", 1);
            await _service.ToggleAsync("u2", 2);

            var ids = await _service.SavedIdsAsync("u1", new[] { 1, 2, 3 });

            Assert.Equal(new HashSet<int> { 1 }, ids);
            Assert.Empty(await _service.SavedIdsAsync("u1", Array.Empty<int>()));
        }

        [Fact]
        public async Task List_NewestFirst_PublishedOnly()
        {
            _db.SavedOpportunities.AddRange(
                new SavedOpportunity { UserId = "u1", OpportunityId = 1, SavedAt = new DateTime(2026, 1, 1) },
                new SavedOpportunity { UserId = "u1", OpportunityId = 2, SavedAt = new DateTime(2026, 2, 1) },
                new SavedOpportunity { UserId = "u1", OpportunityId = 3, SavedAt = new DateTime(2026, 3, 1) }, // مسودة
                new SavedOpportunity { UserId = "u2", OpportunityId = 1, SavedAt = new DateTime(2026, 4, 1) });
            await _db.SaveChangesAsync();

            var list = await _service.ListAsync("u1");

            Assert.Equal(new[] { 2, 1 }, list.Select(o => o.Id));
        }

        [Fact]
        public async Task DeletingTheOpportunity_RemovesItsSavedRows()
        {
            await _service.ToggleAsync("u1", 1);

            _db.Opportunities.Remove(_db.Opportunities.Find(1)!);
            await _db.SaveChangesAsync();

            Assert.Empty(_db.SavedOpportunities);
        }
    }
}
