using System.Security.Cryptography;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>سطر في لوحة سفراء الشهر — الاسم الأول فقط حفاظًا على الخصوصية.</summary>
    public record AmbassadorRow(string FirstName, int Referrals);

    public interface IReferralService
    {
        /// <summary>يضمن أن للمستخدم رمز إحالة (يولّده عند أول طلب) ويعيده.</summary>
        Task<string> EnsureCodeAsync(ApplicationUser user);
        /// <summary>يربط المستخدم الجديد بصاحب الرمز. يتجاهل الرموز المجهولة ويمنع الإحالة الذاتية.</summary>
        Task<bool> ApplyAsync(ApplicationUser newUser, string? code);
        Task<int> CountAsync(string userId);
        /// <summary>أعلى الداعين خلال الشهر الحالي.</summary>
        Task<List<AmbassadorRow>> MonthlyBoardAsync(int take);
    }

    public class ReferralService : IReferralService
    {
        private readonly ApplicationDbContext _db;

        /// <summary>أحرف بلا التباس بصري (لا 0/O ولا 1/I/L).</summary>
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        public const int CodeLength = 8;
        /// <summary>عدد الإحالات لشارة "سفير".</summary>
        public const int AmbassadorThreshold = 3;

        public ReferralService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string> EnsureCodeAsync(ApplicationUser user)
        {
            if (!string.IsNullOrEmpty(user.ReferralCode)) return user.ReferralCode;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var code = GenerateCode();
                if (await _db.Users.AnyAsync(u => u.ReferralCode == code)) continue;
                user.ReferralCode = code;
                await _db.SaveChangesAsync();
                return code;
            }
            throw new InvalidOperationException("Could not generate a unique referral code");
        }

        public async Task<bool> ApplyAsync(ApplicationUser newUser, string? code)
        {
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length != CodeLength || newUser.ReferredByUserId != null) return false;

            var referrer = await _db.Users.FirstOrDefaultAsync(u => u.ReferralCode == code);
            if (referrer == null || referrer.Id == newUser.Id) return false;

            newUser.ReferredByUserId = referrer.Id;
            newUser.ReferredAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<int> CountAsync(string userId) =>
            _db.Users.CountAsync(u => u.ReferredByUserId == userId);

        public async Task<List<AmbassadorRow>> MonthlyBoardAsync(int take)
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var top = await _db.Users
                .Where(u => u.ReferredByUserId != null && u.ReferredAt >= monthStart)
                .GroupBy(u => u.ReferredByUserId!)
                .Select(g => new { ReferrerId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(take)
                .ToListAsync();
            if (top.Count == 0) return new List<AmbassadorRow>();

            var ids = top.Select(t => t.ReferrerId).ToList();
            var names = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.FirstName) ? u.FullName : u.FirstName);

            return top.Select(t => new AmbassadorRow(names.GetValueOrDefault(t.ReferrerId, "—"), t.Count)).ToList();
        }

        public static string GenerateCode()
        {
            var chars = new char[CodeLength];
            for (var i = 0; i < CodeLength; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            return new string(chars);
        }
    }
}
