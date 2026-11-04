using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>نتيجة مطابقة فرصة واحدة بملف البوصلة: درجة للترتيب، وحتى ثلاثة أسباب ملموسة، وسبب نقص واحد قابل للعمل.</summary>
    public sealed class CompassMatch
    {
        public Opportunity Opportunity { get; init; } = null!;
        public int Score { get; init; }
        public List<string> Reasons { get; init; } = new();
        /// <summary>ما ينقصك (شهادة لغة مثلًا)؛ null = مؤهَّل الآن.</summary>
        public string? Gap { get; init; }
        public bool Eligible => Gap == null;
        public int? DaysLeft { get; init; }
    }

    /// <summary>
    /// منطق «بوصلة» الصافي (بلا قاعدة بيانات): الفرصة تُستبعد عند مخالفة صريحة (الدولة، المرحلة، العمر، للنساء، الهدف،
    /// التمويل، عن بُعد)، وتُعدّ "جزئية" عند نقص شهادة لغة، وتُرتَّب بالدرجة. ما لا نعرفه عن الطالب أو الفرصة لا يُحاسَب عليه.
    /// </summary>
    public static class CompassMatcher
    {
        public static readonly HashSet<string> ArabIso = new(StringComparer.OrdinalIgnoreCase)
        {
            "DZ", "BH", "KM", "DJ", "EG", "IQ", "JO", "KW", "LB", "LY", "MR",
            "MA", "OM", "PS", "QA", "SA", "SO", "SD", "SY", "TN", "AE", "YE"
        };

        public static DegreeLevels? ParseDegree(string? degree) =>
            !string.IsNullOrWhiteSpace(degree) && Enum.TryParse<DegreeLevels>(degree.Trim(), true, out var d) && d != DegreeLevels.None && d != DegreeLevels.Any
                ? d : null;

        public static CompassMatch? Evaluate(CompassAnswers a, Opportunity o, DateTime today, string lang, bool includeClosed = false)
        {
            string T(string ar, string en, string fr) => lang == "en" ? en : lang == "fr" ? fr : ar;
            var reasons = new List<(int Weight, string Text)>();
            string? gap = null;
            var score = 0;
            today = today.Date;

            // الموعد النهائي
            int? daysLeft = null;
            if (o.DeadlineType == DeadlineType.SpecificDate && o.EndDate.HasValue)
            {
                daysLeft = (o.EndDate.Value.Date - today).Days;
                if (daysLeft < 0 && !includeClosed) return null;
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
            if (codes.Count > 0)
            {
                if (codes.Contains(FacetParser.WorldCode))
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
            }

            // المرحلة الدراسية
            var level = ParseDegree(a.Degree);
            if (level != null && o.DegreeLevels != DegreeLevels.None && o.DegreeLevels != DegreeLevels.Any)
            {
                if (!o.DegreeLevels.HasFlag(level.Value)) return null;
                score += 15;
                reasons.Add((75, T("مرحلتك الدراسية مقبولة", "Your degree level qualifies", "Votre niveau d'études est accepté")));
            }

            // العمر
            if (a.BirthYear is > 0 && (o.MinAge.HasValue || o.MaxAge.HasValue))
            {
                var age = today.Year - a.BirthYear.Value;
                if ((o.MinAge.HasValue && age < o.MinAge.Value) || (o.MaxAge.HasValue && age > o.MaxAge.Value)) return null;
                score += 10;
                reasons.Add((55, o.MaxAge.HasValue
                    ? T($"عمرك ضمن الحد ({o.MaxAge} سنة)", $"Within the age limit ({o.MaxAge})", $"Dans la limite d'âge ({o.MaxAge} ans)")
                    : T("عمرك مناسب", "Age requirement met", "Âge requis respecté")));
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
            }

            // التمويل
            if (a.FullyFundedOnly && o.FundingType is FundingType.PartiallyFunded or FundingType.SelfFunded or FundingType.Paid) return null;
            if (o.FundingType == FundingType.FullyFunded)
            {
                score += 10;
                reasons.Add((72, T("ممولة بالكامل", "Fully funded", "Entièrement financée")));
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

            // اللغة: سبب إن كانت شهادتك تكفي، ونقص قابل للعمل إن لم تكن
            void Language(bool has, string name)
            {
                if (has)
                {
                    score += 10;
                    reasons.Add((58, T($"شهادتك ({name}) تكفي", $"Your {name} certificate counts", $"Votre certificat {name} suffit")));
                }
                else
                {
                    var need = string.IsNullOrWhiteSpace(o.LanguageScore) ? name : $"{name} · {o.LanguageScore}";
                    gap ??= T($"تحتاج شهادة {need}", $"Needs a {need} certificate", $"Nécessite un certificat {need}");
                }
            }
            switch (o.LanguageRequirement)
            {
                case LanguageRequirement.None:
                    score += 5;
                    reasons.Add((52, T("بلا شهادة لغة", "No language test required", "Sans test de langue")));
                    break;
                case LanguageRequirement.IELTS: Language(a.HasIelts, "IELTS"); break;
                case LanguageRequirement.TOEFL: Language(a.HasToefl, "TOEFL"); break;
                case LanguageRequirement.Either: Language(a.HasIelts || a.HasToefl, "IELTS/TOEFL"); break;
                case LanguageRequirement.French: Language(a.French, "DELF/TCF"); break;
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
                DaysLeft = daysLeft
            };
        }

        private static bool IsArab(string? iso) => iso != null && ArabIso.Contains(iso);
    }
}
