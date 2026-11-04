using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class OpportunityReportServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly OpportunityReportService _service;

        public OpportunityReportServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _service = new OpportunityReportService(_db);

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "admin", UserName = "admin", Email = "admin@test.local" });
            _db.Roles.Add(new IdentityRole { Id = "r-admin", Name = "Admin", NormalizedName = "ADMIN" });
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin", RoleId = "r-admin" });
            _db.Opportunities.Add(NewOpportunity(1));
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id) => new()
        {
            Id = id,
            TitleAr = "t", TitleEn = "t", TitleFr = "t",
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x",
            CreatedByUserId = "u1", AvailableCountries = new List<Country>()
        };

        [Fact]
        public void ReporterKey_MemberUsesId_GuestUsesHash()
        {
            Assert.Equal("u:u1", OpportunityReportService.ReporterKeyFor("u1", "1.2.3.4", "ua"));
            var g1 = OpportunityReportService.ReporterKeyFor(null, "1.2.3.4", "ua");
            var g2 = OpportunityReportService.ReporterKeyFor(null, "1.2.3.4", "ua");
            var g3 = OpportunityReportService.ReporterKeyFor(null, "5.6.7.8", "ua");
            Assert.StartsWith("g:", g1);
            Assert.Equal(42, g1.Length);
            Assert.Equal(g1, g2);
            Assert.NotEqual(g1, g3);
        }

        [Fact]
        public async Task SecondDistinctReporterHidesApplyAndNotifiesAdmin()
        {
            var first = await _service.SubmitAsync(1, OpportunityReportReason.BrokenLink, "  404  ", "g:aaa", null, "me@test.local");
            Assert.NotNull(first);
            Assert.True(first!.Added);
            Assert.Equal(1, first.OpenReporters);
            Assert.False(first.ApplyHidden);
            Assert.False(await _service.IsApplyHiddenAsync(1));
            Assert.Empty(_db.Notifications);

            var duplicate = await _service.SubmitAsync(1, OpportunityReportReason.Expired, null, "g:aaa", null, null);
            Assert.False(duplicate!.Added);
            Assert.Equal(1, duplicate.OpenReporters);
            Assert.Equal(1, await _db.OpportunityReports.CountAsync());

            var second = await _service.SubmitAsync(1, OpportunityReportReason.AsksForMoney, null, "u:u1", "u1", null);
            Assert.True(second!.Added);
            Assert.Equal(2, second.OpenReporters);
            Assert.True(second.ApplyHidden);
            Assert.True(await _service.IsApplyHiddenAsync(1));

            var notification = Assert.Single(_db.Notifications);
            Assert.Equal("admin", notification.UserId);
            Assert.Equal(NotificationKind.OpportunityReported, notification.Kind);
            Assert.Equal(1, notification.OpportunityId);
            Assert.Equal(2, notification.Count);

            var stored = await _db.OpportunityReports.OrderBy(r => r.Id).FirstAsync();
            Assert.Equal("404", stored.Details);
            Assert.Equal("me@test.local", stored.Email);
        }

        [Fact]
        public async Task ResolvingAllReopensApplyAndReadsAdminNotification()
        {
            await _service.SubmitAsync(1, OpportunityReportReason.BrokenLink, null, "g:aaa", null, null);
            await _service.SubmitAsync(1, OpportunityReportReason.BrokenLink, null, "g:bbb", null, null);
            Assert.True(await _service.IsApplyHiddenAsync(1));

            var closed = await _service.ResolveAllAsync(1, OpportunityReportStatus.Resolved, "admin");
            Assert.Equal(2, closed);
            Assert.False(await _service.IsApplyHiddenAsync(1));
            Assert.Equal(0, await _service.OpenCountAsync());
            Assert.All(_db.OpportunityReports, r => { Assert.Equal(OpportunityReportStatus.Resolved, r.Status); Assert.Equal("admin", r.ResolvedByUserId); Assert.NotNull(r.ResolvedAt); });
            Assert.All(_db.Notifications, n => Assert.NotNull(n.ReadAt));
        }

        [Fact]
        public async Task ResolvingOneOfTwoKeepsApplyHidden()
        {
            await _service.SubmitAsync(1, OpportunityReportReason.BrokenLink, null, "g:aaa", null, null);
            await _service.SubmitAsync(1, OpportunityReportReason.BrokenLink, null, "g:bbb", null, null);
            await _service.SubmitAsync(1, OpportunityReportReason.Other, null, "g:ccc", null, null);
            var id = await _db.OpportunityReports.Select(r => r.Id).FirstAsync();

            Assert.True(await _service.ResolveAsync(id, OpportunityReportStatus.Dismissed, "admin"));
            Assert.True(await _service.IsApplyHiddenAsync(1)); // ما زال مُبلِّغان مفتوحان
            Assert.False(await _service.ResolveAsync(id, OpportunityReportStatus.Open, "admin"));
            Assert.False(await _service.ResolveAsync(9999, OpportunityReportStatus.Resolved, "admin"));
        }

        [Fact]
        public async Task UnknownOpportunityReturnsNull()
        {
            Assert.Null(await _service.SubmitAsync(42, OpportunityReportReason.Other, null, "g:aaa", null, null));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
