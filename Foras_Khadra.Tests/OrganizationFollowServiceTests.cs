using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class OrganizationFollowServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly OrganizationFollowService _service;

        public OrganizationFollowServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _service = new OrganizationFollowService(_db);

            _db.Users.Add(new ApplicationUser { Id = "org1", UserName = "org1", Email = "org1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "m1", UserName = "m1", Email = "m1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "m2", UserName = "m2", Email = "m2@test.local" });
            var org = new Organization
            {
                Id = 7, Name = "جمعية البيئة", Sector = "env", Country = "JO", ContactName = "c", ContactEmail = "c@test.local",
                PhoneNumber = "0", Password = "x", UserId = "org1", IsManual = false
            };
            _db.Organizations.Add(org);
            _db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = ""; // internal set
            _db.SaveChanges();
            _db.Opportunities.Add(NewOpportunity(1, "org1", OpportunityStatus.Published));
            _db.Opportunities.Add(NewOpportunity(2, "org1", OpportunityStatus.PendingReview));
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id, string creator, OpportunityStatus status) => new()
        {
            Id = id,
            TitleAr = "t", TitleEn = "t", TitleFr = "t",
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x",
            CreatedByUserId = creator, Status = status, AvailableCountries = new List<Country>()
        };

        [Fact]
        public async Task ToggleFollowsThenUnfollows()
        {
            Assert.True(await _service.ToggleAsync("m1", 7));
            Assert.True(await _service.IsFollowingAsync("m1", 7));
            Assert.Equal(1, await _service.FollowerCountAsync(7));

            Assert.False(await _service.ToggleAsync("m1", 7));
            Assert.False(await _service.IsFollowingAsync("m1", 7));
            Assert.Equal(0, await _service.FollowerCountAsync(7));

            Assert.False(await _service.ToggleAsync("m1", 999)); // منظمة غير موجودة
        }

        [Fact]
        public async Task PublishedOpportunityNotifiesEachFollowerOnce()
        {
            await _service.ToggleAsync("m1", 7);
            await _service.ToggleAsync("m2", 7);

            Assert.Equal(2, await _service.NotifyFollowersAsync(1));
            Assert.Equal(0, await _service.NotifyFollowersAsync(1)); // لا تكرار

            var notifications = await _db.Notifications.OrderBy(n => n.UserId).ToListAsync();
            Assert.Equal(2, notifications.Count);
            Assert.All(notifications, n =>
            {
                Assert.Equal(NotificationKind.OrganizationPublished, n.Kind);
                Assert.Equal(1, n.OpportunityId);
                Assert.Equal("جمعية البيئة", n.Label);
                Assert.Equal("7", n.LinkParams);
            });
        }

        [Fact]
        public async Task UnpublishedOrUnknownOpportunityNotifiesNobody()
        {
            await _service.ToggleAsync("m1", 7);
            Assert.Equal(0, await _service.NotifyFollowersAsync(2));
            Assert.Equal(0, await _service.NotifyFollowersAsync(404));
            Assert.Empty(_db.Notifications);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
