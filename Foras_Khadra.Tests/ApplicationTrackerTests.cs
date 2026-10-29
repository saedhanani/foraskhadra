using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>تتبّع التقديم ورفيق التقديم و"تابعت مؤخرًا" على SQLite في الذاكرة بالمخطط الحقيقي.</summary>
    public class ApplicationTrackerTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly ApplicationTracker _tracker;

        public ApplicationTrackerTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _tracker = new ApplicationTracker(_db);

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "u2", UserName = "u2", Email = "u2@test.local" });
            _db.Opportunities.Add(NewOpportunity(1, OpportunityType.Scholarships));
            _db.Opportunities.Add(NewOpportunity(2, OpportunityType.Jobs));
            _db.Opportunities.Add(NewOpportunity(3, OpportunityType.Competitions));
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "u1", OpportunityId = 1 });
            _db.SavedOpportunities.Add(new SavedOpportunity { UserId = "u1", OpportunityId = 2 });
            _db.SaveChanges();
        }

        private static Opportunity NewOpportunity(int id, OpportunityType type) => new()
        {
            Id = id,
            TitleAr = "x", TitleEn = "x", TitleFr = "x",
            DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
            DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
            EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
            BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
            ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x", CreatedByUserId = "u1",
            AvailableCountries = new List<Country>(),
            Status = OpportunityStatus.Published,
            Type = type
        };

        // ===== 2-1 حالة التقديم =====

        [Fact]
        public async Task SetStatus_OnlyForOwnSavedOpportunity()
        {
            Assert.True(await _tracker.SetStatusAsync("u1", 1, ApplicationStatus.Applied));
            Assert.False(await _tracker.SetStatusAsync("u2", 1, ApplicationStatus.Applied)); // ليست محفوظة عند u2
            Assert.False(await _tracker.SetStatusAsync("u1", 3, ApplicationStatus.Applied)); // غير محفوظة

            var saved = _db.SavedOpportunities.Single(s => s.UserId == "u1" && s.OpportunityId == 1);
            Assert.Equal(ApplicationStatus.Applied, saved.Status);
            Assert.NotNull(saved.StatusChangedAt);
        }

        [Fact]
        public async Task SetStatus_AppliedCompletesExistingChecklist()
        {
            var saved = await _tracker.GetWithChecklistAsync("u1", 1);
            Assert.NotNull(saved);
            Assert.All(saved!.Checklist, c => Assert.False(c.Done));

            await _tracker.SetStatusAsync("u1", 1, ApplicationStatus.Applied);

            var items = _db.ChecklistItems.Where(c => c.SavedOpportunityId == saved.Id).ToList();
            Assert.NotEmpty(items);
            Assert.All(items, c => Assert.True(c.Done));
        }

        [Fact]
        public async Task Stats_CountSavedAppliedAccepted()
        {
            await _tracker.SetStatusAsync("u1", 1, ApplicationStatus.Accepted);
            await _tracker.SetStatusAsync("u1", 2, ApplicationStatus.Pending);

            var stats = await _tracker.StatsAsync("u1");
            Assert.Equal(2, stats.Saved);
            Assert.Equal(2, stats.Applied);
            Assert.Equal(1, stats.Accepted);
            Assert.Equal(new TrackerStats(0, 0, 0), await _tracker.StatsAsync("u2"));
        }

        // ===== 2-2 رفيق التقديم =====

        [Fact]
        public async Task Checklist_DefaultsMaterializeOncePerType()
        {
            var first = await _tracker.GetWithChecklistAsync("u1", 1);
            var again = await _tracker.GetWithChecklistAsync("u1", 1);

            var expected = ChecklistDefaults.For(OpportunityType.Scholarships);
            Assert.Equal(expected, first!.Checklist.OrderBy(c => c.SortOrder).Select(c => c.Key));
            Assert.Equal(expected.Count, again!.Checklist.Count); // لا تتكرر
            Assert.Null(await _tracker.GetWithChecklistAsync("u2", 1)); // ليست محفوظة عند u2
        }

        [Fact]
        public async Task Checklist_ToggleAddRemove_AffectProgress()
        {
            var saved = await _tracker.GetWithChecklistAsync("u1", 2); // وظيفة: 3 بنود
            var firstItem = saved!.Checklist.OrderBy(c => c.SortOrder).First();

            Assert.True(await _tracker.ToggleItemAsync("u1", firstItem.Id));
            Assert.False(await _tracker.ToggleItemAsync("u2", firstItem.Id)); // ليس بنده

            Assert.True(await _tracker.AddItemAsync("u1", 2, "  ترجمة الشهادة  "));
            Assert.False(await _tracker.AddItemAsync("u1", 2, "   "));

            var card = (await _tracker.CardsAsync("u1", new[] { 2 }))[2];
            Assert.Equal(4, card.Total);
            Assert.Equal(1, card.Done);
            Assert.Equal(25, card.Percent);

            var custom = _db.ChecklistItems.Single(c => c.Text == "ترجمة الشهادة");
            Assert.True(await _tracker.RemoveItemAsync("u1", custom.Id));
            Assert.Equal(3, (await _tracker.CardsAsync("u1", new[] { 2 }))[2].Total);
        }

        [Fact]
        public async Task Checklist_RespectsMaxItems()
        {
            await _tracker.GetWithChecklistAsync("u1", 2);
            for (var i = 0; i < ApplicationTracker.MaxChecklistItems; i++)
                await _tracker.AddItemAsync("u1", 2, "item " + i);

            Assert.False(await _tracker.AddItemAsync("u1", 2, "one too many"));
            Assert.Equal(ApplicationTracker.MaxChecklistItems, _db.ChecklistItems.Count());
        }

        [Fact]
        public async Task Card_PercentIs100AfterApplying_EvenWithoutChecklist()
        {
            await _tracker.SetStatusAsync("u1", 1, ApplicationStatus.Pending);
            var card = (await _tracker.CardsAsync("u1", new[] { 1 }))[1];
            Assert.False(card.HasChecklist);
            Assert.Equal(100, card.Percent);
        }

        [Fact]
        public async Task Checklist_DeletedWhenUnsaved()
        {
            var saved = await _tracker.GetWithChecklistAsync("u1", 1);
            Assert.NotEmpty(_db.ChecklistItems.Where(c => c.SavedOpportunityId == saved!.Id));

            _db.SavedOpportunities.Remove(_db.SavedOpportunities.Single(s => s.Id == saved!.Id));
            _db.SaveChanges();

            Assert.Empty(_db.ChecklistItems.Where(c => c.SavedOpportunityId == saved!.Id));
        }

        // ===== 2-3 تابعت مؤخرًا =====

        [Fact]
        public async Task RecentViews_UpsertAndOrderByLatest()
        {
            await _tracker.RecordViewAsync("u1", 1);
            await _tracker.RecordViewAsync("u1", 2);
            await _tracker.RecordViewAsync("u1", 1); // فتح ثانٍ يحدّث الوقت ولا يكرر الصف

            Assert.Equal(2, _db.RecentViews.Count(r => r.UserId == "u1"));
            var recent = await _tracker.RecentAsync("u1", 5);
            Assert.Equal(new[] { 1, 2 }, recent.Select(o => o.Id));
            Assert.Empty(await _tracker.RecentAsync("u2", 5));
        }

        [Fact]
        public async Task RecentViews_TrimmedToMax()
        {
            for (var i = 10; i < 10 + ApplicationTracker.MaxRecentViews + 5; i++)
            {
                _db.Opportunities.Add(NewOpportunity(i, OpportunityType.Jobs));
                _db.SaveChanges();
                await _tracker.RecordViewAsync("u1", i);
            }

            Assert.Equal(ApplicationTracker.MaxRecentViews, _db.RecentViews.Count(r => r.UserId == "u1"));
        }

        // ===== 3-3 مراقبة الدورة القادمة =====

        [Fact]
        public async Task CycleWatch_ToggleOnOff_AndOnlyPublished()
        {
            Assert.True(await _tracker.ToggleCycleWatchAsync("u1", 3));
            Assert.True(await _tracker.IsWatchingAsync("u1", 3));
            Assert.False(await _tracker.IsWatchingAsync("u2", 3));

            Assert.False(await _tracker.ToggleCycleWatchAsync("u1", 3)); // إلغاء
            Assert.False(await _tracker.IsWatchingAsync("u1", 3));

            Assert.False(await _tracker.ToggleCycleWatchAsync("u1", 999)); // غير موجودة
        }

        // ===== 2-4 ملف التقويم =====

        [Fact]
        public void Calendar_BuildsValidEventWithAlarm()
        {
            var ics = CalendarHelper.BuildDeadlineEvent(42, "منحة; مع, فاصلة", new DateTime(2026, 11, 15), "https://foraskhadra.com/Opportunity/Details/42", new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc));

            Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
            Assert.EndsWith("END:VCALENDAR\r\n", ics);
            Assert.Contains("UID:opportunity-42@foraskhadra.com\r\n", ics);
            Assert.Contains("DTSTART;VALUE=DATE:20261115\r\n", ics);
            Assert.Contains("DTEND;VALUE=DATE:20261116\r\n", ics);
            Assert.Contains("SUMMARY:منحة\\; مع\\, فاصلة\r\n", ics);
            Assert.Contains("TRIGGER:-P3D\r\n", ics);
            Assert.Contains("DTSTAMP:20261029T100000Z\r\n", ics);
        }

        [Fact]
        public void Calendar_FoldsLongLinesAt75Bytes()
        {
            var longTitle = new string('a', 100);
            var folded = CalendarHelper.Fold("SUMMARY:" + longTitle);
            var lines = folded.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 2);
            Assert.All(lines, l => Assert.True(System.Text.Encoding.UTF8.GetByteCount(l) <= 75));
            Assert.StartsWith(" ", lines[1]); // سطر الاستمرار يبدأ بمسافة
            Assert.Equal("SUMMARY:" + longTitle, folded.Replace("\r\n ", "").TrimEnd('\r', '\n'));
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
