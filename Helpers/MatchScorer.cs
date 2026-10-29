using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>ما نعرفه عن المستخدم للترشيح: اهتماماته (كأنواع فرص) وجنسيته وبلد إقامته (كمعرّفات في جدول Countries).</summary>
    public record MatchProfile(IReadOnlyList<OpportunityType> InterestTypes, int? NationalityCountryId, int? ResidenceCountryId)
    {
        public bool HasInterests => InterestTypes.Count > 0;
        public bool HasLocation => NationalityCountryId.HasValue || ResidenceCountryId.HasValue;
    }

    /// <summary>منطق المرحلة 3 الصافي: ربط جنسية المستخدم بدول الفرصة (3-1) ونسبة التطابق (3-2). بلا قاعدة بيانات — انظر MatchService.</summary>
    public static class MatchScorer
    {
        public const int AllWorldId = 1;
        public const int ArabWorldId = 2;

        /// <summary>مقاطع (مطبّعة) تظهر في أسماء RegionInfo ولا تطابق أسماء الجدول حرفيًا.</summary>
        private static readonly (string fragment, int countryId)[] Aliases =
        {
            ("palestin", 3), ("فلسطين", 3),
            ("syrian", 5), ("السوري", 5),
            ("saudi", 8), ("السعودي", 8),
            ("emirates", 12), ("uae", 12), ("الامارات", 12),
            ("morocc", 20), ("المغرب", 20),
            ("yabuuti", 22), ("djibouti", 22), ("جيبوتي", 22),
            ("comor", 24), ("القمر", 24)
        };

        /// <summary>يحوّل اسم جنسية/بلد كما خزّنه التسجيل (NativeName أو EnglishName من RegionInfo) إلى دولة من الجدول، أو null إن لم تكن عربية/معروفة.</summary>
        public static Country? ResolveCountry(string? name, IEnumerable<Country> countries)
        {
            var n = SearchIndexer.Normalize(name);
            if (n.Length == 0) return null;

            var list = countries.Where(c => c.Id != AllWorldId && c.Id != ArabWorldId).ToList();

            // تطابق تام أولًا، ثم احتواء ككلمة كاملة ("المملكة العربية السعودية" تحوي كلمة "السعودية")؛
            // الاحتواء الحرفي خطر: "romania" تحوي "oman"
            var padded = " " + n + " ";
            foreach (var c in list)
                if (Names(c).Any(cn => cn == n)) return c;
            foreach (var c in list)
                if (Names(c).Any(cn => cn.Length >= 3 && padded.Contains(" " + cn + " "))) return c;

            foreach (var (fragment, id) in Aliases)
                if (n.Contains(fragment))
                    return list.FirstOrDefault(c => c.Id == id);

            return null;

            static IEnumerable<string> Names(Country c) =>
                new[] { c.NameAr, c.NameEn, c.NameFr }.Select(SearchIndexer.Normalize).Where(s => s.Length > 0);
        }

        /// <summary>هل تقبل الفرصة جنسية/بلد المستخدم؟ true/false، أو null حين لا نعرف (لا دول على الفرصة، أو لا موقع للمستخدم).</summary>
        public static bool? Eligible(MatchProfile profile, Opportunity opp)
        {
            var ids = opp.AvailableCountries?.Select(c => c.Id).ToHashSet();
            if (ids == null || ids.Count == 0) return null;
            if (ids.Contains(AllWorldId)) return true;
            if (!profile.HasLocation) return null;
            if (ids.Contains(ArabWorldId)) return true; // كل دول الجدول عربية
            return (profile.NationalityCountryId.HasValue && ids.Contains(profile.NationalityCountryId.Value))
                || (profile.ResidenceCountryId.HasValue && ids.Contains(profile.ResidenceCountryId.Value));
        }

        /// <summary>نسبة التطابق 0–100 على المعايير المعروفة فقط: الاهتمام (50)، قبول الجنسية (35)، الانفتاح (15: مفتوحة/تنتهي قريبًا/منتهية).</summary>
        public static int Score(MatchProfile profile, Opportunity opp, DateTime? today = null)
        {
            var day = (today ?? DateTime.Now).Date;
            int earned = 0, applicable = 0;

            if (profile.HasInterests)
            {
                applicable += 50;
                if (opp.Type.HasValue && profile.InterestTypes.Contains(opp.Type.Value)) earned += 50;
            }

            var eligible = Eligible(profile, opp);
            if (eligible.HasValue)
            {
                applicable += 35;
                if (eligible.Value) earned += 35;
            }

            applicable += 15;
            var hasDeadline = opp.DeadlineType == DeadlineType.SpecificDate && opp.EndDate.HasValue;
            if (!hasDeadline || opp.EndDate!.Value.Date > day.AddDays(7)) earned += 15;
            else if (opp.EndDate.Value.Date >= day) earned += 8;

            return applicable == 0 ? 0 : (int)Math.Round(earned * 100.0 / applicable);
        }
    }
}
