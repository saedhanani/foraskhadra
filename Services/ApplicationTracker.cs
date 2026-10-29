using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>ملخص تتبّع التقديم لبطاقة فرصة محفوظة: الحالة وتقدّم رفيق التقديم.</summary>
    public record TrackerCard(int OpportunityId, ApplicationStatus Status, int Done, int Total)
    {
        /// <summary>100 بعد التقديم مهما كانت البنود؛ وإلا نسبة المنجز من البنود (0 إن لم تُفتح القائمة بعد).</summary>
        public int Percent => Status.HasApplied() ? 100 : Total == 0 ? 0 : Done * 100 / Total;
        public bool HasChecklist => Total > 0;
    }

    public record TrackerStats(int Saved, int Applied, int Accepted);

    public interface IApplicationTracker
    {
        // ===== 2-1 حالة التقديم =====
        Task<bool> SetStatusAsync(string userId, int opportunityId, ApplicationStatus status);
        Task<Dictionary<int, TrackerCard>> CardsAsync(string userId, IEnumerable<int> opportunityIds);
        Task<TrackerStats> StatsAsync(string userId);

        // ===== 2-2 رفيق التقديم =====
        /// <summary>الفرصة المحفوظة مع بنودها؛ تُنشأ البنود الافتراضية عند أول فتح. null إن لم تكن محفوظة.</summary>
        Task<SavedOpportunity?> GetWithChecklistAsync(string userId, int opportunityId);
        Task<bool> ToggleItemAsync(string userId, int itemId);
        Task<bool> AddItemAsync(string userId, int opportunityId, string text);
        Task<bool> RemoveItemAsync(string userId, int itemId);

        // ===== 2-3 تابعت مؤخرًا =====
        Task RecordViewAsync(string userId, int opportunityId);
        Task<List<Opportunity>> RecentAsync(string userId, int take);
    }

    public class ApplicationTracker : IApplicationTracker
    {
        private readonly ApplicationDbContext _db;

        public const int MaxChecklistItems = 20;
        public const int MaxRecentViews = 20;

        public ApplicationTracker(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> SetStatusAsync(string userId, int opportunityId, ApplicationStatus status)
        {
            var saved = await _db.SavedOpportunities
                .Include(s => s.Checklist)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OpportunityId == opportunityId);
            if (saved == null) return false;

            saved.Status = status;
            saved.StatusChangedAt = DateTime.Now;

            // "قدّمت" فما بعد: الملف مكتمل بحكم الواقع
            if (status.HasApplied())
                foreach (var item in saved.Checklist) item.Done = true;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<int, TrackerCard>> CardsAsync(string userId, IEnumerable<int> opportunityIds)
        {
            var ids = opportunityIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, TrackerCard>();

            var rows = await _db.SavedOpportunities
                .Where(s => s.UserId == userId && ids.Contains(s.OpportunityId))
                .Select(s => new
                {
                    s.OpportunityId,
                    s.Status,
                    Done = s.Checklist.Count(c => c.Done),
                    Total = s.Checklist.Count
                })
                .AsNoTracking()
                .ToListAsync();

            return rows.ToDictionary(r => r.OpportunityId, r => new TrackerCard(r.OpportunityId, r.Status, r.Done, r.Total));
        }

        public async Task<TrackerStats> StatsAsync(string userId)
        {
            var q = _db.SavedOpportunities.Where(s => s.UserId == userId && s.Opportunity!.Status == OpportunityStatus.Published);
            return new TrackerStats(
                await q.CountAsync(),
                await q.CountAsync(s => s.Status != ApplicationStatus.NotApplied),
                await q.CountAsync(s => s.Status == ApplicationStatus.Accepted));
        }

        public async Task<SavedOpportunity?> GetWithChecklistAsync(string userId, int opportunityId)
        {
            var saved = await _db.SavedOpportunities
                .Include(s => s.Opportunity!).ThenInclude(o => o.AvailableCountries)
                .Include(s => s.Checklist)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OpportunityId == opportunityId);
            if (saved == null) return null;

            if (saved.Checklist.Count == 0)
            {
                var order = 0;
                foreach (var key in ChecklistDefaults.For(saved.Opportunity?.Type))
                    saved.Checklist.Add(new ChecklistItem { Key = key, SortOrder = order++, Done = saved.Status.HasApplied() });
                await _db.SaveChangesAsync();
            }

            return saved;
        }

        public async Task<bool> ToggleItemAsync(string userId, int itemId)
        {
            var item = await _db.ChecklistItems
                .FirstOrDefaultAsync(c => c.Id == itemId && c.SavedOpportunity!.UserId == userId);
            if (item == null) return false;

            item.Done = !item.Done;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddItemAsync(string userId, int opportunityId, string text)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0) return false;
            if (text.Length > 120) text = text[..120];

            var saved = await _db.SavedOpportunities
                .Include(s => s.Checklist)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OpportunityId == opportunityId);
            if (saved == null || saved.Checklist.Count >= MaxChecklistItems) return false;

            var nextOrder = saved.Checklist.Count == 0 ? 0 : saved.Checklist.Max(c => c.SortOrder) + 1;
            saved.Checklist.Add(new ChecklistItem { Text = text, SortOrder = nextOrder });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveItemAsync(string userId, int itemId)
        {
            var removed = await _db.ChecklistItems
                .Where(c => c.Id == itemId && c.SavedOpportunity!.UserId == userId)
                .ExecuteDeleteAsync();
            return removed > 0;
        }

        public async Task RecordViewAsync(string userId, int opportunityId)
        {
            var existing = await _db.RecentViews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.OpportunityId == opportunityId);

            if (existing != null)
            {
                existing.ViewedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return;
            }

            _db.RecentViews.Add(new RecentView { UserId = userId, OpportunityId = opportunityId });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // فتحان متزامنان: الفهرس الفريد رفض الثاني — لا شيء يُفعل
                _db.ChangeTracker.Clear();
                return;
            }

            // نبقي أحدث 20 فقط لكل مستخدم
            var stale = await _db.RecentViews
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ViewedAt)
                .Skip(MaxRecentViews)
                .Select(r => r.Id)
                .ToListAsync();
            if (stale.Count > 0)
                await _db.RecentViews.Where(r => stale.Contains(r.Id)).ExecuteDeleteAsync();
        }

        public async Task<List<Opportunity>> RecentAsync(string userId, int take)
        {
            var rows = await _db.RecentViews
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Opportunity!.Status == OpportunityStatus.Published)
                .OrderByDescending(r => r.ViewedAt)
                .Take(take)
                .Include(r => r.Opportunity!).ThenInclude(o => o.AvailableCountries)
                .ToListAsync();
            return rows.Select(r => r.Opportunity!).ToList();
        }
    }
}
