using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>دورة طلب النشر: وصول → إشعار المديرين ورسالة الاستلام → ربط بفرصة → نشر يُغلق الطلب ويراسل المنظمة.</summary>
    public class PublishRequestServiceTests : IDisposable
    {
        private sealed class FakeEmailQueue : IEmailQueue
        {
            public List<(string To, string Subject, string Html)> Sent { get; } = new();
            public void Enqueue(string to, string subject, string htmlBody) => Sent.Add((to, subject, htmlBody));
        }

        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly FakeEmailQueue _emails = new();
        private readonly PublishRequestService _service;

        public PublishRequestServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["BaseUrl"] = "https://www.foraskhadra.com/" }).Build();
            _service = new PublishRequestService(_db, _emails, cfg);

            _db.Users.Add(new ApplicationUser { Id = "admin1", UserName = "admin1", Email = "admin1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "member", UserName = "member", Email = "member@test.local" });
            _db.Roles.Add(new IdentityRole { Id = "r-admin", Name = "Admin", NormalizedName = "ADMIN" });
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = "admin1", RoleId = "r-admin" });
            _db.SaveChanges();
        }

        private static PublishRequest NewRequest(string lang = "en") => new()
        {
            OrganizationName = "Green Org", ContactEmail = "org@example.org", SourceUrl = "https://example.org/call",
            Language = lang, EstimatedReach = 1200
        };

        private Opportunity AddOpportunity(int id, OpportunityStatus status)
        {
            var o = new Opportunity
            {
                Id = id, TitleAr = "منحة خضراء", TitleEn = "Green grant", TitleFr = "Bourse verte",
                DescriptionAr = "", DescriptionEn = "", DescriptionFr = "", DetailsAr = "", DetailsEn = "", DetailsFr = "",
                EligibilityCriteriaAr = "", EligibilityCriteriaEn = "", EligibilityCriteriaFr = "", BenefitsAr = "", BenefitsEn = "", BenefitsFr = "",
                ApplyLink = "https://example.org/apply", ImagePath = "", PublishedBy = "Admin", CreatedByUserId = "member", Status = status, PublishDate = DateTime.Now
            };
            _db.Opportunities.Add(o);
            _db.SaveChanges();
            return o;
        }

        [Fact]
        public async Task Create_NotifiesAdminsAndEmailsOrganization()
        {
            var r = await _service.CreateAsync(NewRequest("fr"));

            Assert.Equal(PublishRequestStatus.New, r.Status);
            var notif = Assert.Single(_db.Notifications.Where(n => n.Kind == NotificationKind.PublishRequestNew));
            Assert.Equal("admin1", notif.UserId);
            Assert.Equal(r.Id.ToString(), notif.LinkParams);
            Assert.Equal("Green Org", notif.Label);

            var mail = Assert.Single(_emails.Sent);
            Assert.Equal("org@example.org", mail.To);
            Assert.Contains("Foras Khadra", mail.Subject);
            Assert.Contains("1", mail.Html); // التقدير يظهر
            Assert.Contains("/fr/for-organizations", mail.Html);
            Assert.Equal(1, await _service.OpenCountAsync());
        }

        [Fact]
        public async Task Link_ThenPublish_ClosesRequestAndEmailsLink()
        {
            var r = await _service.CreateAsync(NewRequest("en"));
            var draft = AddOpportunity(10, OpportunityStatus.Draft);
            _emails.Sent.Clear();

            Assert.True(await _service.LinkOpportunityAsync(r.Id, draft.Id));
            Assert.Equal(PublishRequestStatus.InProgress, r.Status);
            Assert.Equal(10, r.OpportunityId);
            Assert.Empty(_emails.Sent); // لم تُنشر بعد

            draft.Status = OpportunityStatus.Published;
            _db.SaveChanges();
            await _service.MarkPublishedAsync(10);

            Assert.Equal(PublishRequestStatus.Published, r.Status);
            Assert.NotNull(r.ClosedAt);
            var mail = Assert.Single(_emails.Sent);
            Assert.Contains("https://www.foraskhadra.com/en/Opportunity/Details/10/", mail.Html);
            Assert.Contains("Green grant", mail.Html);
            Assert.Empty(_db.Notifications.Where(n => n.Kind == NotificationKind.PublishRequestNew && n.ReadAt == null));
            Assert.Equal(0, await _service.OpenCountAsync());
        }

        [Fact]
        public async Task Link_ToAlreadyPublished_ClosesImmediately()
        {
            var r = await _service.CreateAsync(NewRequest("ar"));
            var live = AddOpportunity(11, OpportunityStatus.Published);
            _emails.Sent.Clear();

            Assert.True(await _service.LinkOpportunityAsync(r.Id, live.Id));
            Assert.Equal(PublishRequestStatus.Published, r.Status);
            var mail = Assert.Single(_emails.Sent);
            Assert.Contains("https://www.foraskhadra.com/Opportunity/Details/11/", mail.Html); // العربية بلا بادئة
        }

        [Fact]
        public async Task MarkPublished_IgnoresUnlinkedOpportunities()
        {
            await _service.CreateAsync(NewRequest());
            AddOpportunity(12, OpportunityStatus.Published);
            _emails.Sent.Clear();
            await _service.MarkPublishedAsync(12);
            Assert.Empty(_emails.Sent);
            Assert.Equal(1, await _service.OpenCountAsync());
        }

        [Fact]
        public async Task Reject_EmailsReason_SpamDoesNot()
        {
            var a = await _service.CreateAsync(NewRequest());
            var b = await _service.CreateAsync(NewRequest());
            _emails.Sent.Clear();

            Assert.True(await _service.RejectAsync(a.Id, "expired call", spam: false));
            Assert.True(await _service.RejectAsync(b.Id, null, spam: true));

            Assert.Equal(PublishRequestStatus.Rejected, a.Status);
            Assert.Equal("expired call", a.AdminNote);
            Assert.Equal(PublishRequestStatus.Spam, b.Status);
            var mail = Assert.Single(_emails.Sent);
            Assert.Contains("expired call", mail.Html);
            Assert.Equal(0, await _service.OpenCountAsync());
        }

        [Fact]
        public async Task Take_OnlyMovesNewToInProgress()
        {
            var r = await _service.CreateAsync(NewRequest());
            Assert.True(await _service.TakeAsync(r.Id));
            Assert.Equal(PublishRequestStatus.InProgress, r.Status);
            Assert.NotNull(r.TakenAt);
            Assert.False(await _service.TakeAsync(r.Id));
            Assert.False(await _service.TakeAsync(999));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
