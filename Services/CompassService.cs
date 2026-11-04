using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Services
{
    /// <summary>برنامج متكرر مغلق الآن يناسب الملف: "DAAD يفتح عادةً في مارس — ابدأ التجهيز".</summary>
    public sealed class SoonProgram
    {
        public Opportunity LastCycle { get; init; } = null!;
        public DateTime? ExpectedOpen { get; init; }
        public int? MonthsUntil { get; init; }
        public List<string> Reasons { get; init; } = new();
    }

    /// <summary>ورقة من قائمة الجاهزية: هل عندك، وكم فرصة تفتحها لو جهّزتها.</summary>
    public sealed record ReadinessItem(string Key, string Label, bool Have, int Unlocks);

    /// <summary>بند في خطة الأسبوع: موعد قريب، أو فرصة جديدة، أو ورقة تُجهَّز.</summary>
    public sealed record WeekItem(string Kind, string Text, string? Url, int? OpportunityId);

    public sealed class CompassResult
    {
        public List<CompassMatch> Now { get; init; } = new();
        public int NowCount => Now.Count(m => m.Eligible);
        public int PartialCount => Now.Count(m => !m.Eligible);
        public int NewThisWeek { get; init; }
        public List<SoonProgram> Soon { get; init; } = new();
        public int SoonWithin3Months => Soon.Count(s => s.MonthsUntil is <= 3);
        public List<ReadinessItem> Readiness { get; init; } = new();
        public int ReadinessPercent { get; init; }
        public List<WeekItem> Week { get; init; } = new();
    }

    public interface ICompassService
    {
        Task<CompassResult> BuildAsync(CompassAnswers answers, string lang, CancellationToken ct = default);
        Task<EligibilityProfile?> GetProfileAsync(string userId, CancellationToken ct = default);
        /// <summary>يحفظ الملف (وينشئه)، ويكمل بلد المستخدم وجنسيته واهتماماته إن كانت فارغة.</summary>
        Task<EligibilityProfile> SaveAsync(ApplicationUser user, CompassAnswers answers, CancellationToken ct = default);
        Task<bool> SetDocumentAsync(string userId, string key, bool have, CancellationToken ct = default);
    }

    /// <summary>
    /// يبني نتيجة البوصلة من الفرص المفتوحة (مخزَّنة 10 دقائق) وملف الأهلية: الآن / قريبًا / جاهزيتك / خطة الأسبوع.
    /// </summary>
    public class CompassService : ICompassService
    {
        private const string OpenKey = "compass:open";
        private const string ProgramsKey = "compass:programs";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
        public const int MaxNow = 60;
        public const int MaxSoon = 8;

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public CompassService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<CompassResult> BuildAsync(CompassAnswers answers, string lang, CancellationToken ct = default)
        {
            var a = answers.Normalized();
            var today = DateTime.Today;
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;

            var open = await OpenOpportunitiesAsync(ct);
            var now = open.Select(o => CompassMatcher.Evaluate(a, o, today, lang))
                .Where(m => m != null).Select(m => m!)
                .OrderByDescending(m => m.Eligible).ThenByDescending(m => m.Score).ThenBy(m => m.DaysLeft ?? int.MaxValue)
                .Take(MaxNow)
                .ToList();
            var eligible = now.Where(m => m.Eligible).ToList();
            var openKeys = open.Where(o => o.ProgramKey != null).Select(o => o.ProgramKey!).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // قريبًا: آخر دورة مغلقة من كل برنامج متكرر لا دورة مفتوحة له الآن
            var soon = new List<SoonProgram>();
            foreach (var last in await ClosedProgramCyclesAsync(ct))
            {
                if (openKeys.Contains(last.ProgramKey!)) continue;
                var m = CompassMatcher.Evaluate(a, last, today, lang, includeClosed: true);
                if (m == null) continue;
                var expected = last.PublishDate.Date.AddYears(1);
                int? months = expected >= today ? Math.Max(0, (expected.Year - today.Year) * 12 + expected.Month - today.Month) : null;
                soon.Add(new SoonProgram { LastCycle = last, ExpectedOpen = months.HasValue ? expected : null, MonthsUntil = months, Reasons = m.Reasons });
            }
            soon = soon.OrderBy(s => s.MonthsUntil ?? int.MaxValue).ThenByDescending(s => s.LastCycle.PublishDate).Take(MaxSoon).ToList();

            // جاهزيتك: أوراق أهدافك (أو أنواع الفرص المؤهَّل لها) — وكم فرصة تفتح كل ورقة ناقصة
            var goalTypes = a.GoalTypes();
            if (goalTypes.Count == 0) goalTypes = eligible.Where(m => m.Opportunity.Type.HasValue).Select(m => m.Opportunity.Type!.Value).Distinct().ToList();
            var needed = goalTypes.SelectMany(t => ChecklistDefaults.For(t)).Distinct().ToList();
            var have = a.Documents.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var readiness = needed.Select(key => new ReadinessItem(key, ChecklistDefaults.Label(key, lang), have.Contains(key),
                    have.Contains(key) ? 0 : eligible.Count(m => ChecklistDefaults.For(m.Opportunity.Type).Contains(key))))
                .ToList();
            var readinessPercent = needed.Count == 0 ? 0 : readiness.Count(r => r.Have) * 100 / needed.Count;

            // خطة الأسبوع: ثلاثة بنود كحد أقصى
            var week = new List<WeekItem>();
            var closing = eligible.Where(m => m.DaysLeft is >= 0 and <= 30).OrderBy(m => m.DaysLeft).FirstOrDefault();
            if (closing != null)
                week.Add(new WeekItem("deadline",
                    T($"ينتهي خلال {closing.DaysLeft} يومًا: {Title(closing.Opportunity, lang)}", $"Closes in {closing.DaysLeft} days: {Title(closing.Opportunity, lang)}", $"Se clôture dans {closing.DaysLeft} jours : {Title(closing.Opportunity, lang)}"),
                    null, closing.Opportunity.Id));
            var fresh = eligible.Where(m => m.Opportunity.PublishDate >= today.AddDays(-7) && m.Opportunity.Id != closing?.Opportunity.Id).OrderByDescending(m => m.Opportunity.PublishDate).FirstOrDefault();
            if (fresh != null)
                week.Add(new WeekItem("new",
                    T("جديدة هذا الأسبوع: " + Title(fresh.Opportunity, lang), "New this week: " + Title(fresh.Opportunity, lang), "Nouveau cette semaine : " + Title(fresh.Opportunity, lang)),
                    null, fresh.Opportunity.Id));
            var doc = readiness.Where(r => !r.Have && r.Unlocks > 0).OrderByDescending(r => r.Unlocks).FirstOrDefault();
            if (doc != null)
                week.Add(new WeekItem("document",
                    T($"جهّز «{doc.Label}» وتصبح جاهزًا لـ{doc.Unlocks} فرصة", $"Prepare “{doc.Label}” and you are ready for {doc.Unlocks} opportunities", $"Préparez « {doc.Label} » et vous serez prêt pour {doc.Unlocks} opportunités"),
                    null, null));

            return new CompassResult
            {
                Now = now,
                NewThisWeek = eligible.Count(m => m.Opportunity.PublishDate >= today.AddDays(-7)),
                Soon = soon,
                Readiness = readiness,
                ReadinessPercent = readinessPercent,
                Week = week
            };
        }

        public Task<EligibilityProfile?> GetProfileAsync(string userId, CancellationToken ct = default) =>
            _db.EligibilityProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        public async Task<EligibilityProfile> SaveAsync(ApplicationUser user, CompassAnswers answers, CancellationToken ct = default)
        {
            var a = answers.Normalized();
            var profile = await _db.EligibilityProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (profile == null)
            {
                profile = new EligibilityProfile { UserId = user.Id };
                _db.EligibilityProfiles.Add(profile);
            }
            profile.Apply(a, includeDocuments: a.Documents.Count > 0 || profile.Documents == null);

            // السؤال الذكي بالاتجاه المعاكس: ما أجاب عنه هنا يكمل ملفه القديم (الترشيح والإشعارات تقرأ منه)
            var countries = await _db.Countries.AsNoTracking().ToListAsync(ct);
            string? NameFor(string? iso) => iso == null ? null : countries.FirstOrDefault(c => string.Equals(c.IsoCode, iso, StringComparison.OrdinalIgnoreCase))?.NameAr ?? CountryHelper.NameForIso(iso, "ar");
            if (string.IsNullOrWhiteSpace(user.Country) && a.Residence != null) user.Country = NameFor(a.Residence);
            if (string.IsNullOrWhiteSpace(user.Nationality) && a.Nationality != null) user.Nationality = NameFor(a.Nationality);
            if (user.Interests.Count == 0 && a.Goals.Count > 0)
                user.Interests = a.GoalTypes().Select(t => InterestMap.ToType.FirstOrDefault(kv => kv.Value == t).Key).Where(k => k != null).Distinct().ToList();
            _db.Users.Update(user);

            await _db.SaveChangesAsync(ct);
            return profile;
        }

        public async Task<bool> SetDocumentAsync(string userId, string key, bool have, CancellationToken ct = default)
        {
            key = (key ?? "").Trim().ToLowerInvariant();
            if (key.Length is 0 or > 40) return false;
            var profile = await _db.EligibilityProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (profile == null) return false;
            var docs = EligibilityProfile.Split(profile.Documents);
            if (have && !docs.Contains(key, StringComparer.OrdinalIgnoreCase)) docs.Add(key);
            if (!have) docs.RemoveAll(d => string.Equals(d, key, StringComparison.OrdinalIgnoreCase));
            profile.Documents = EligibilityProfile.Csv(docs);
            profile.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        private static string Title(Opportunity o, string lang)
        {
            var t = lang == "en" ? o.TitleEn : lang == "fr" ? o.TitleFr : o.TitleAr;
            return string.IsNullOrWhiteSpace(t) ? o.TitleAr : t;
        }

        /// <summary>الفرص المنشورة المفتوحة مع دولها ومجالاتها — تُحمَّل مرة كل 10 دقائق لكل الزوار.</summary>
        private async Task<List<Opportunity>> OpenOpportunitiesAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(OpenKey, out List<Opportunity>? cached) && cached != null) return cached;
            var today = DateTime.Today;
            var list = await _db.Opportunities.AsNoTracking()
                .Include(o => o.AvailableCountries)
                .Include(o => o.Fields)
                .Where(o => o.Status == OpportunityStatus.Published
                            && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today))
                .OrderByDescending(o => o.PublishDate)
                .ToListAsync(ct);
            _cache.Set(OpenKey, list, Ttl);
            return list;
        }

        /// <summary>آخر دورة منشورة من كل برنامج متكرر (ProgramKey) انتهى موعدها.</summary>
        private async Task<List<Opportunity>> ClosedProgramCyclesAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(ProgramsKey, out List<Opportunity>? cached) && cached != null) return cached;
            var today = DateTime.Today;
            var cycles = await _db.Opportunities.AsNoTracking()
                .Include(o => o.AvailableCountries)
                .Include(o => o.Fields)
                .Where(o => o.Status == OpportunityStatus.Published && o.ProgramKey != null && o.ProgramKey != "")
                .ToListAsync(ct);
            var list = cycles
                .GroupBy(o => o.ProgramKey!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(o => o.PublishDate).First())
                .Where(o => o.DeadlineType == DeadlineType.SpecificDate && o.EndDate.HasValue && o.EndDate.Value.Date < today)
                .ToList();
            _cache.Set(ProgramsKey, list, Ttl);
            return list;
        }
    }
}
