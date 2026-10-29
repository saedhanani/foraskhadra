using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    public interface ISavedOpportunityService
    {
        /// <summary>يحفظ الفرصة إن لم تكن محفوظة ويلغي الحفظ إن كانت. يعيد الحالة الجديدة (true = محفوظة).</summary>
        Task<bool> ToggleAsync(string userId, int opportunityId);
        Task<bool> IsSavedAsync(string userId, int opportunityId);
        /// <summary>معرّفات الفرص المحفوظة من بين قائمة معروضة — استعلام واحد للصفحة كلها.</summary>
        Task<HashSet<int>> SavedIdsAsync(string userId, IEnumerable<int> opportunityIds);
        /// <summary>الفرص المحفوظة للمستخدم من الأحدث حفظًا، المنشورة فقط.</summary>
        Task<List<Opportunity>> ListAsync(string userId);
        /// <summary>كم مستخدمًا حفظ كل فرصة من المعروضة (البند 4-3 "X شخصًا حفظ هذه الفرصة") — المفتاح OpportunityId.</summary>
        Task<Dictionary<int, int>> SaveCountsAsync(IEnumerable<int> opportunityIds);
    }

    public class SavedOpportunityService : ISavedOpportunityService
    {
        private readonly ApplicationDbContext _context;

        public SavedOpportunityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ToggleAsync(string userId, int opportunityId)
        {
            var existing = await _context.SavedOpportunities
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OpportunityId == opportunityId);

            if (existing != null)
            {
                _context.SavedOpportunities.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }

            // المسودات لا تُحفظ: الزائر لا يراها أصلًا
            var publishable = await _context.Opportunities
                .AnyAsync(o => o.Id == opportunityId && o.Status == OpportunityStatus.Published);
            if (!publishable) return false;

            _context.SavedOpportunities.Add(new SavedOpportunity { UserId = userId, OpportunityId = opportunityId });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // نقرتان متزامنتان: الفهرس الفريد رفض الثانية، والنتيجة واحدة — محفوظة
                _context.ChangeTracker.Clear();
            }
            return true;
        }

        public Task<bool> IsSavedAsync(string userId, int opportunityId) =>
            _context.SavedOpportunities.AnyAsync(s => s.UserId == userId && s.OpportunityId == opportunityId);

        public async Task<HashSet<int>> SavedIdsAsync(string userId, IEnumerable<int> opportunityIds)
        {
            var ids = opportunityIds.Distinct().ToList();
            if (ids.Count == 0) return new HashSet<int>();

            var saved = await _context.SavedOpportunities
                .Where(s => s.UserId == userId && ids.Contains(s.OpportunityId))
                .Select(s => s.OpportunityId)
                .ToListAsync();
            return saved.ToHashSet();
        }

        public async Task<Dictionary<int, int>> SaveCountsAsync(IEnumerable<int> opportunityIds)
        {
            var ids = opportunityIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            return await _context.SavedOpportunities
                .Where(s => ids.Contains(s.OpportunityId))
                .GroupBy(s => s.OpportunityId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<List<Opportunity>> ListAsync(string userId)
        {
            // Include ثم Select في الذاكرة: EF لا يقبل Include بعد Select على مسار تنقّل
            var saved = await _context.SavedOpportunities
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.Opportunity!.Status == OpportunityStatus.Published)
                .OrderByDescending(s => s.SavedAt)
                .Include(s => s.Opportunity!).ThenInclude(o => o.AvailableCountries)
                .ToListAsync();
            return saved.Select(s => s.Opportunity!).ToList();
        }
    }
}
