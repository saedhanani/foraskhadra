using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>قاعدة SQLite في الذاكرة بالمخطط الحقيقي — نفس نمط NotificationSweeperTests.</summary>
    public class OpportunityReviewServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly OpportunityReviewService _service;

        public OpportunityReviewServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _service = new OpportunityReviewService(_db);

            _db.Users.Add(new ApplicationUser { Id = "org1", UserName = "org1", Email = "org1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "admin1", UserName = "admin1", Email = "admin1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "admin2", UserName = "admin2", Email = "admin2@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "member", UserName = "member", Email = "member@test.local" });
            _db.Roles.Add(new IdentityRole { Id = "r-admin", Name = "Admin", NormalizedName = "ADMIN" });
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin1", RoleId = "r-admin" });
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin2", RoleId = "r-admin" });
            var org = new Organization
            {
                Id = 1, Name = "Org", Sector = "s", Country = "JO", ContactName = "c", ContactEmail = "org1@test.local",
                PhoneNumber = "0", Password = "x", UserId = "org1"
            };
            _db.Organizations.Add(org);
            _db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = ""; // internal set
            _db.SaveChanges();
        }

        private Opportunity AddOpportunity(int id, OpportunityStatus status, string createdBy = "org1")
        {
            var o = new Opportunity
            {
                Id = id,
                TitleAr = "t", TitleEn = "t", TitleFr = "t",
                DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
                DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
                EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
                BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
                ApplyLink = "https://example.test",
                ImagePath = "x",
                PublishedBy = "Org",
                CreatedByUserId = createdBy,
                AvailableCountries = new List<Country>(),
                Status = status,
                PublishDate = new DateTime(2026, 1, 1)
            };
            _db.Opportunities.Add(o);
            _db.SaveChanges();
            return o;
        }

        [Fact]
        public void UnverifiedOrganizationStartsPending_VerifiedPublishes()
        {
            var org = _db.Organizations.Single();
            Assert.Equal(OpportunityStatus.PendingReview, _service.StatusFor(org));
            org.IsVerified = true;
            Assert.Equal(OpportunityStatus.Published, _service.StatusFor(org));
        }

        [Fact]
        public async Task NotifyAdmins_CreatesOneUnreadNotificationPerAdmin_AndDoesNotDuplicate()
        {
            AddOpportunity(10, OpportunityStatus.PendingReview);

            await _service.NotifyAdminsAsync(10);
            await _service.NotifyAdminsAsync(10);

            var notes = _db.Notifications.Where(n => n.Kind == NotificationKind.ReviewPending).ToList();
            Assert.Equal(2, notes.Count);
            Assert.Equal(new[] { "admin1", "admin2" }, notes.Select(n => n.UserId).OrderBy(x => x));
            Assert.All(notes, n => Assert.Equal(10, n.OpportunityId));
            Assert.DoesNotContain(notes, n => n.UserId == "member");
        }

        [Fact]
        public async Task Approve_PublishesSetsPublishDate_ClearsNote_ReadsNotifications_AndCanVerifyOrg()
        {
            var opp = AddOpportunity(11, OpportunityStatus.Rejected);
            opp.ReviewNote = "old reason";
            _db.SaveChanges();
            await _service.NotifyAdminsAsync(11);

            var before = DateTime.Now.AddSeconds(-1);
            Assert.True(await _service.ApproveAsync(11, verifyOrganization: true));

            var saved = _db.Opportunities.Single(o => o.Id == 11);
            Assert.Equal(OpportunityStatus.Published, saved.Status);
            Assert.True(saved.PublishDate >= before);
            Assert.Null(saved.ReviewNote);
            Assert.NotNull(saved.ReviewedAt);
            Assert.True(_db.Organizations.Single().IsVerified);
            Assert.All(_db.Notifications.Where(n => n.OpportunityId == 11), n => Assert.NotNull(n.ReadAt));
        }

        [Fact]
        public async Task Approve_RefusesAlreadyPublishedOrSuspended()
        {
            AddOpportunity(12, OpportunityStatus.Published);
            AddOpportunity(13, OpportunityStatus.Suspended);
            Assert.False(await _service.ApproveAsync(12, false));
            Assert.False(await _service.ApproveAsync(13, false));
            Assert.False(await _service.ApproveAsync(999, false));
        }

        [Fact]
        public async Task Reject_StoresTrimmedNote_AndHidesFromPublic()
        {
            AddOpportunity(14, OpportunityStatus.PendingReview);

            Assert.True(await _service.RejectAsync(14, "  الرابط لا يعمل  "));

            var saved = _db.Opportunities.Single(o => o.Id == 14);
            Assert.Equal(OpportunityStatus.Rejected, saved.Status);
            Assert.Equal("الرابط لا يعمل", saved.ReviewNote);
            Assert.Empty(_db.Opportunities.Where(o => o.Status == OpportunityStatus.Published));
        }

        [Fact]
        public async Task Reject_TruncatesLongNote()
        {
            AddOpportunity(15, OpportunityStatus.PendingReview);
            await _service.RejectAsync(15, new string('x', OpportunityReviewService.MaxNoteLength + 50));
            Assert.Equal(OpportunityReviewService.MaxNoteLength, _db.Opportunities.Single(o => o.Id == 15).ReviewNote!.Length);
        }

        [Fact]
        public async Task Suspend_HidesPublishedOnly_AndUnsuspendRestoresThem()
        {
            AddOpportunity(16, OpportunityStatus.Published);
            AddOpportunity(17, OpportunityStatus.PendingReview);
            AddOpportunity(18, OpportunityStatus.Published, createdBy: "member"); // منظمة أخرى لا تتأثر

            Assert.True(await _service.SetOrganizationSuspendedAsync(1, true));
            Assert.True(_db.Organizations.Single().IsSuspended);
            Assert.Equal(OpportunityStatus.Suspended, _db.Opportunities.Single(o => o.Id == 16).Status);
            Assert.Equal(OpportunityStatus.PendingReview, _db.Opportunities.Single(o => o.Id == 17).Status);
            Assert.Equal(OpportunityStatus.Published, _db.Opportunities.Single(o => o.Id == 18).Status);

            Assert.True(await _service.SetOrganizationSuspendedAsync(1, false));
            Assert.False(_db.Organizations.Single().IsSuspended);
            Assert.Equal(OpportunityStatus.Published, _db.Opportunities.Single(o => o.Id == 16).Status);
            Assert.Equal(OpportunityStatus.PendingReview, _db.Opportunities.Single(o => o.Id == 17).Status);
        }

        [Fact]
        public async Task PendingCount_CountsOnlyPendingReview()
        {
            AddOpportunity(19, OpportunityStatus.PendingReview);
            AddOpportunity(20, OpportunityStatus.PendingReview);
            AddOpportunity(21, OpportunityStatus.Draft);
            AddOpportunity(22, OpportunityStatus.Rejected);
            Assert.Equal(2, await _service.PendingCountAsync());
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
