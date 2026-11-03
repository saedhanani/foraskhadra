using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>يعدّ نقرات «قدّم الآن» (بوابة /go/{id}) في صف يومي واحد لكل فرصة.</summary>
    public interface IApplyClickCounter
    {
        Task CountAsync(int opportunityId, DateOnly? day = null, CancellationToken ct = default);
    }

    public class ApplyClickCounter : IApplyClickCounter
    {
        private readonly ApplicationDbContext _db;

        public ApplyClickCounter(ApplicationDbContext db) => _db = db;

        public async Task CountAsync(int opportunityId, DateOnly? day = null, CancellationToken ct = default)
        {
            var d = day ?? DateOnly.FromDateTime(DateTime.Now);
            var row = await _db.OpportunityDailyStats.FindAsync(new object[] { opportunityId, d }, ct);
            if (row != null)
            {
                row.ApplyClicks++;
                await _db.SaveChangesAsync(ct);
                return;
            }

            _db.OpportunityDailyStats.Add(new OpportunityDailyStat { OpportunityId = opportunityId, Day = d, ApplyClicks = 1 });
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // سباق: طلب آخر أنشأ صف اليوم في اللحظة نفسها — نزيد عليه بدل أن نفشل
                _db.ChangeTracker.Clear();
                var existing = await _db.OpportunityDailyStats.FindAsync(new object[] { opportunityId, d }, ct);
                if (existing == null) return;
                existing.ApplyClicks++;
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
