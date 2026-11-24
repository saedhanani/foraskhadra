using System.Globalization;
using System.Text.RegularExpressions;
using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// نتيجة مطابقة فرصة واحدة بملف البوصلة: درجة للترتيب، وحتى ثلاثة أسباب ملموسة، وسبب نقص واحد قابل للعمل،
    /// وقائمة بالشروط التي لم نستطع التحقق منها (بيانات ناقصة في الفرصة أو في إجابات الطالب).
    /// ثلاث درجات: مؤكَّدة (Confirmed) / تحتاج مراجعة (Eligible بلا تأكيد) / ينقصها شرط (Gap).
    /// </summary>
    public sealed class CompassMatch
    {
        public Opportunity Opportunity { get; init; } = null!;
        public int Score { get; init; }
        public List<string> Reasons { get; init; } = new();
        /// <summary>ما ينقصك (شهادة لغة أو درجة أعلى)؛ null = لا نقص معروف.</summary>
        public string? Gap { get; init; }
        /// <summary>شروط لم نتحقق منها: الفرصة لا تذكرها في بياناتها، أو الطالب لم يجب عنها.</summary>
        public List<string> Verify { get; init; } = new();
        /// <summary>لا نقص معروف (مؤكَّدة أو تحتاج مراجعة).</summary>
        public bool Eligible => Gap == null;
        /// <summary>كل شرط نعرفه عن الفرصة تحققنا منه بإجابة من الطالب، ولا شرط أساسي مجهول.</summary>
        public bool Confirmed => Gap == null && Verify.Count == 0;
        public int? DaysLeft { get; init; }
    }

    /// <summary>
    /// منطق «بوصلة» الصافي (بلا قاعدة بيانات): الفرصة تُستبعد عند مخالفة صريحة (الدولة، المرحلة، العمر، للنساء، الهدف،
    /// التمويل، عن بُعد)، وتُعدّ "جزئية" عند نقص شهادة لغة أو درجتها، وتُرتَّب بالدرجة. ما لا نعرفه عن الطالب أو الفرصة
    /// لا يستبعدها، لكنه يُسجَّل في Verify فلا تُحسب "مؤكَّدة".
    /// </summary>
    public static class CompassMatcher
    {
        public static readonly HashSet<string> ArabIso = new(StringComparer.OrdinalIgnoreCase)
        {
            "DZ", "BH", "KM", "DJ", "EG", "IQ", "JO", "KW", "LB", "LY", "MR",
            "MA", "OM", "PS", "QA", "SA", "SO", "SD", "SY", "TN", "AE", "YE"
        };

        /// <summary>فرصة بلا موعد معلن تُعدّ قديمة بعد هذه المدة من نشرها ولا تظهر في «الآن».</summary>
        public const int StaleAfterDays = 180;
        /// <summary>فرصة بلا موعد معلن يُطلب من الطالب التأكد أنها ما زالت مفتوحة بعد هذه المدة.</summary>
        public const int VerifyOpenAfterDays = 45;

        // رقم مستقل: "B2" و"C1" (مستويات CEFR) ليست درجات
        private static readonly Regex Number = new(@"(?<![A-Za-z\d.,])\d+(?:[.,]\d+)?(?![A-Za-z\d])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex TitleYear = new(@"(?<!\d)20\d\d(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static DegreeLevels? ParseDegree(string? degree) =>
            !string.IsNullOrWhiteSpace(degree) && Enum.TryParse<DegreeLevels>(degree.Trim(), true, out var d) && d != DegreeLevels.None && d != DegreeLevels.Any
                ? d : null;

        /// <summary>أول رقم في النص يقع في نطاق الاختبار: IELTS من 1 إلى 9، TOEFL iBT من 20 إلى 120.</summary>
        public static decimal? ParseScore(string? text, bool ielts)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            foreach (Match m in Number.Matches(text))
            {
                if (!decimal.TryParse(m.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) continue;
                if (ielts ? v is >= 1 and <= 9 : v is >= 20 and <= 120) return v;
            }
            return null;
        }

        private static bool HasFixedDeadline(Opportunity o) => o.DeadlineType == DeadlineType.SpecificDate && o.EndDate.HasValue;

        /// <summary>
        /// فرصة بلا موعد معلن يُرجَّح أنها أُغلقت: نُشرت قبل أكثر من <see cref="StaleAfterDays"/> يومًا،
        /// أو أحدث سنة في عنوانها مضت ("منحة … 2025" ونحن في 2026).
        /// </summary>
        public static bool IsStale(Opportunity o, DateTime today)
        {
            if (HasFixedDeadline(o)) return false;
            if (o.PublishDate.Date < today.Date.AddDays(-StaleAfterDays)) return true;
            var years = TitleYear.Matches((o.TitleEn ?? "") + " " + (o.TitleAr ?? "")).Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();
            return years.Count > 0 && years.Max() < today.Year;
        }

        public static CompassMatch? Evaluate(CompassAnswers a, Opportunity o, DateTime today, string lang, bool includeClosed = false)
        {
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
            var reasons = new List<(int Weight, string Text)>();
            var verify = new List<string>();
            string? gap = null;
            var score = 0;
            today = today.Date;

            // الموعد النهائي
            int? daysLeft = null;
            if (HasFixedDeadline(o))
            {
                daysLeft = (o.EndDate!.Value.Date - today).Days;
                if (daysLeft < 0 && !includeClosed) return null;
            }
            else if (!includeClosed)
            {
                if (IsStale(o, today)) return null;
                if (o.PublishDate.Date < today.AddDays(-VerifyOpenAfterDays))
                    verify.Add(T("بلا موعد معلن — تأكد أنها ما زالت مفتوحة", "No stated deadline — check it is still open", "Sans date limite annoncée — vérifiez qu'elle est encore ouverte"));
            }

            // الهدف: ما تريده فقط
            if (a.Goals.Count > 0 && o.Type.HasValue)
            {
                if (!a.Goals.Contains(o.Type.Value.ToString(), StringComparer.OrdinalIgnoreCase)) return null;
                score += 20;
                reasons.Add((90, T("هدفك: " + o.Type.Value.GetDisplayName("ar"), "Your goal: " + o.Type.Value.GetDisplayName("en"), "Votre objectif : " + o.Type.Value.GetDisplayName("fr"))));
            }

            // الدولة: XW للجميع، XA للوطن العربي، وإلا رموز محددة
            var codes = FacetParser.EligibleCodes(o).Select(c => c.ToUpperInvariant()).ToHashSet();
            var res = a.Residence;
            var nat = a.Nationality;
            var hasLocation = res != null || nat != null;
            if (codes.Count == 0)
            {
                verify.Add(T("الدول المؤهَّلة غير محددة في بياناتنا", "Eligible countries are not specified in our data", "Les pays éligibles ne sont pas précisés dans nos données"));
            }
            else if (codes.Contains(FacetParser.WorldCode))
            {
                score += 10;
                reasons.Add((50, T("مفتوحة لكل العالم", "Open worldwide", "Ouverte au monde entier")));
            }
            else if ((res != null && codes.Contains(res)) || (nat != null && codes.Contains(nat)))
            {
                score += 25;
                reasons.Add((80, T("تقبل المتقدمين من بلدك", "Accepts applicants from your country", "Accepte les candidats de votre pays")));
            }
            else if (codes.Contains(FacetParser.ArabWorldCode) && (IsArab(res) || IsArab(nat)))
            {
                score += 20;
                reasons.Add((70, T("مفتوحة لدول الوطن العربي", "Open to the Arab world", "Ouverte au monde arabe")));
            }
            else if (hasLocation)
            {
                return null; // دول محددة لا تشمل بلدك ولا جنسيتك
            }
            else
            {
                verify.Add(T("لدول محددة — أضف بلدك وجنسيتك لنتحقق", "Limited to certain countries — add your country and nationality so we can check", "Réservée à certains pays — ajoutez votre pays et votre nationalité"));
            }

            // المرحلة الدراسية
            var level = ParseDegree(a.Degree);
            if (o.DegreeLevels == DegreeLevels.None)
            {
                verify.Add(T("المرحلة الدراسية المطلوبة غير محددة في بياناتنا", "The required degree level is not specified in our data", "Le niveau d'études requis n'est pas précisé dans nos données"));
            }
            else if (o.DegreeLevels != DegreeLevels.Any)
            {
                if (level == null)
                {
                    verify.Add(T("تشترط مرحلة دراسية — أضف مرحلتك لنتحقق", "Has a degree requirement — add your level so we can check", "Exige un niveau d'études — ajoutez le vôtre"));
                }
                else
                {
                    if (!o.DegreeLevels.HasFlag(level.Value)) return null;
                    score += 15;
                    reasons.Add((75, T("مرحلتك الدراسية مقبولة", "Your degree level qualifies", "Votre niveau d'études est accepté")));
                    // لم يتخرج بعد وأمامه أكثر من سنة: المنح والزمالات تشترط غالبًا إتمام المرحلة قبل البدء
                    if (a.GraduationYear > today.Year + 1 && o.Type is OpportunityType.Scholarships or OpportunityType.Fellowships)
                        verify.Add(T($"تتخرج في {a.GraduationYear} — تأكد أنها تقبل من لم يُتمّ مرحلته بعد", $"You graduate in {a.GraduationYear} — check it accepts applicants who have not finished yet", $"Vous serez diplômé en {a.GraduationYear} — vérifiez qu'elle accepte les candidats non encore diplômés"));
                }
            }

            // العمر
            if (o.MinAge.HasValue || o.MaxAge.HasValue)
            {
                if (a.BirthYear is > 0)
                {
                    var age = today.Year - a.BirthYear.Value;
                    if ((o.MinAge.HasValue && age < o.MinAge.Value) || (o.MaxAge.HasValue && age > o.MaxAge.Value)) return null;
                    score += 10;
                    reasons.Add((55, o.MaxAge.HasValue
                        ? T($"عمرك ضمن الحد ({o.MaxAge} سنة)", $"Within the age limit ({o.MaxAge})", $"Dans la limite d'âge ({o.MaxAge} ans)")
                        : T("عمرك مناسب", "Age requirement met", "Âge requis respecté")));
                }
                else
                {
                    verify.Add(T("لها حدّ عمر — أضف سنة ميلادك لنتحقق", "Has an age limit — add your year of birth so we can check", "Limite d'âge — ajoutez votre année de naissance"));
                }
            }

            // للنساء فقط
            if (o.ForWomenOnly)
            {
                if (a.Gender == "m") return null;
                if (a.Gender == "f")
                {
                    score += 10;
                    reasons.Add((65, T("مخصصة للنساء", "For women", "Réservée aux femmes")));
                }
                else verify.Add(T("مخصصة للنساء", "For women only", "Réservée aux femmes"));
            }

            // التمويل
            if (a.FullyFundedOnly && o.FundingType is FundingType.PartiallyFunded or FundingType.SelfFunded or FundingType.Paid) return null;
            if (o.FundingType == FundingType.FullyFunded)
            {
                score += 10;
                reasons.Add((72, T("ممولة بالكامل", "Fully funded", "Entièrement financée")));
            }
            else if (a.FullyFundedOnly && o.FundingType == FundingType.Unknown)
            {
                verify.Add(T("نوع التمويل غير محدد في بياناتنا", "Funding type is not specified in our data", "Le type de financement n'est pas précisé dans nos données"));
            }

            // عن بُعد
            var remote = o.IsRemote || o.IsOnline;
            if (a.RemoteOnly && !remote) return null;
            if (remote && a.RemoteOnly)
            {
                score += 5;
                reasons.Add((40, T("عن بُعد", "Remote / online", "À distance / en ligne")));
            }

            // المجال
            var fields = o.Fields?.ToList() ?? new List<Field>();
            if (a.Fields.Count > 0 && fields.Count > 0 && !fields.Any(f => f.Key == Field.AnyKey))
            {
                var common = fields.FirstOrDefault(f => a.Fields.Contains(f.Key, StringComparer.OrdinalIgnoreCase));
                if (common != null)
                {
                    score += 15;
                    reasons.Add((85, T("في مجالك: " + common.NameAr, "In your field: " + common.NameEn, "Dans votre domaine : " + common.NameFr)));
                }
                else score -= 5;
            }

            // اللغة: سبب إن كانت شهادتك (ودرجتك) تكفي، ونقص قابل للعمل إن لم تكن
            void MissingCertificate(string name)
            {
                var need = string.IsNullOrWhiteSpace(o.LanguageScore) ? name : $"{name} · {o.LanguageScore}";
                gap ??= T($"تحتاج شهادة {need}", $"Needs a {need} certificate", $"Nécessite un certificat {need}");
            }
            void CertificateCounts(string name)
            {
                score += 10;
                reasons.Add((58, T($"شهادتك ({name}) تكفي", $"Your {name} certificate counts", $"Votre certificat {name} suffit")));
            }
            // true = الدرجة تكفي، false = أقل من المطلوب، null = لا نستطيع المقارنة (درجة مطلوبة أو درجة الطالب غير معروفة)
            bool? ScoreEnough(bool ielts, out decimal? required, out decimal? mine)
            {
                required = ParseScore(o.LanguageScore, ielts);
                mine = ParseScore(a.EnglishScore, ielts);
                if (required == null) return true;
                return mine == null ? null : mine >= required;
            }
            void English(bool acceptIelts, bool acceptToefl)
            {
                var held = new List<(string Name, bool Ielts)>();
                if (acceptIelts && a.HasIelts) held.Add(("IELTS", true));
                if (acceptToefl && a.HasToefl) held.Add(("TOEFL", false));
                if (held.Count == 0)
                {
                    MissingCertificate(acceptIelts && acceptToefl ? "IELTS/TOEFL" : acceptIelts ? "IELTS" : "TOEFL");
                    return;
                }
                string? low = null, unknown = null;
                foreach (var (name, ielts) in held)
                {
                    var enough = ScoreEnough(ielts, out var required, out var mine);
                    if (enough == true) { CertificateCounts(name); return; }
                    if (enough == false)
                        low ??= T($"درجتك في {name} ({Fmt(mine)}) أقل من المطلوب ({Fmt(required)})", $"Your {name} score ({Fmt(mine)}) is below the required {Fmt(required)}", $"Votre score {name} ({Fmt(mine)}) est inférieur au minimum requis ({Fmt(required)})");
                    else
                        unknown ??= T($"المطلوب {name} {Fmt(required)} — أضف درجتك لنتحقق", $"Requires {name} {Fmt(required)} — add your score so we can check", $"Exige {name} {Fmt(required)} — ajoutez votre score");
                }
                if (unknown != null) { score += 5; verify.Add(unknown); }
                else gap ??= low;
            }
            switch (o.LanguageRequirement)
            {
                case LanguageRequirement.None:
                    score += 5;
                    reasons.Add((52, T("بلا شهادة لغة", "No language test required", "Sans test de langue")));
                    break;
                case LanguageRequirement.IELTS: English(true, false); break;
                case LanguageRequirement.TOEFL: English(false, true); break;
                case LanguageRequirement.Either: English(true, true); break;
                case LanguageRequirement.French:
                    if (a.French) CertificateCounts("DELF/TCF"); else MissingCertificate("DELF/TCF");
                    break;
                case LanguageRequirement.Other:
                    verify.Add(string.IsNullOrWhiteSpace(o.LanguageScore)
                        ? T("تشترط شهادة لغة أخرى — راجع الشروط", "Requires another language certificate — check the requirements", "Exige un autre certificat de langue — vérifiez les conditions")
                        : T($"تشترط شهادة لغة: {o.LanguageScore}", $"Requires a language certificate: {o.LanguageScore}", $"Exige un certificat de langue : {o.LanguageScore}"));
                    break;
                default:
                    verify.Add(T("شرط اللغة غير محدد في بياناتنا", "The language requirement is not specified in our data", "L'exigence linguistique n'est pas précisée dans nos données"));
                    break;
            }

            // بلد الاستضافة المفضّل
            if (a.HostCountries.Count > 0 && !string.IsNullOrEmpty(o.HostCountryIso) && a.HostCountries.Contains(o.HostCountryIso, StringComparer.OrdinalIgnoreCase))
            {
                score += 10;
                reasons.Add((45, T("في بلد تفضّله", "In a country you prefer", "Dans un pays que vous préférez")));
            }

            // الوقت المتبقي
            if (daysLeft is >= 14)
            {
                score += Math.Min(10, daysLeft.Value / 7);
                reasons.Add((30, T($"أمامك {daysLeft} يومًا", $"{daysLeft} days left", $"{daysLeft} jours restants")));
            }
            else if (daysLeft is >= 0) score += 3;

            return new CompassMatch
            {
                Opportunity = o,
                Score = Math.Max(0, score),
                Reasons = reasons.OrderByDescending(r => r.Weight).Take(3).Select(r => r.Text).ToList(),
                Gap = gap,
                Verify = verify,
                DaysLeft = daysLeft
            };
        }

        private static string Fmt(decimal? v) => v?.ToString("0.#", CultureInfo.InvariantCulture) ?? "";

        private static bool IsArab(string? iso) => iso != null && ArabIso.Contains(iso);
    }
}
