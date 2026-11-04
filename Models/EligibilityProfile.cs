using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Foras_Khadra.Helpers;

namespace Foras_Khadra.Models
{
    /// <summary>
    /// ملف أهلية العضو (1:1 مع المستخدم) — ما تحتاجه «بوصلة» لتقول "تستطيع التقديم الآن": البلد والجنسية والعمر
    /// والمرحلة والمجال واللغة والهدف، وقائمة الأوراق الجاهزة. القوائم تُخزَّن كقيم مفصولة بفاصلة.
    /// </summary>
    public class EligibilityProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [StringLength(2)] public string? ResidenceIso { get; set; }
        [StringLength(2)] public string? NationalityIso { get; set; }
        public int? BirthYear { get; set; }
        /// <summary>f أو m أو null (اختياري؛ لفرص النساء فقط).</summary>
        [StringLength(1)] public string? Gender { get; set; }

        /// <summary>عضو DegreeLevels واحد (HighSchool…Professional).</summary>
        [StringLength(20)] public string? Degree { get; set; }
        [StringLength(400)] public string? FieldKeys { get; set; }
        public int? GraduationYear { get; set; }
        [StringLength(150)] public string? University { get; set; }
        /// <summary>ielts,toefl</summary>
        [StringLength(40)] public string? EnglishTests { get; set; }
        [StringLength(20)] public string? EnglishScore { get; set; }
        public bool FrenchCertificate { get; set; }
        public int? ExperienceYears { get; set; }

        /// <summary>أسماء OpportunityType مفصولة بفاصلة.</summary>
        [StringLength(200)] public string? Goals { get; set; }
        [StringLength(200)] public string? HostCountries { get; set; }
        public bool FullyFundedOnly { get; set; }
        public bool RemoteOnly { get; set; }
        public int? StartWithinMonths { get; set; }

        /// <summary>مفاتيح الأوراق الجاهزة (ChecklistDefaults) — "جاهزيتك".</summary>
        [StringLength(400)] public string? Documents { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public CompassAnswers ToAnswers() => new()
        {
            Residence = ResidenceIso, Nationality = NationalityIso, BirthYear = BirthYear, Gender = Gender,
            Degree = Degree, Fields = Split(FieldKeys), GraduationYear = GraduationYear, University = University,
            English = Split(EnglishTests), EnglishScore = EnglishScore, French = FrenchCertificate, ExperienceYears = ExperienceYears,
            Goals = Split(Goals), HostCountries = Split(HostCountries), FullyFundedOnly = FullyFundedOnly, RemoteOnly = RemoteOnly,
            StartWithinMonths = StartWithinMonths, Documents = Split(Documents)
        };

        /// <summary>يطبّق الإجابات (بعد تطبيعها) على الملف؛ قائمة الأوراق لا تُمسّ إلا إن أُرسلت.</summary>
        public void Apply(CompassAnswers a, bool includeDocuments = false)
        {
            a = a.Normalized();
            ResidenceIso = a.Residence; NationalityIso = a.Nationality; BirthYear = a.BirthYear; Gender = a.Gender;
            Degree = a.Degree; FieldKeys = Csv(a.Fields); GraduationYear = a.GraduationYear; University = a.University;
            EnglishTests = Csv(a.English); EnglishScore = a.EnglishScore; FrenchCertificate = a.French; ExperienceYears = a.ExperienceYears;
            Goals = Csv(a.Goals); HostCountries = Csv(a.HostCountries); FullyFundedOnly = a.FullyFundedOnly; RemoteOnly = a.RemoteOnly;
            StartWithinMonths = a.StartWithinMonths;
            if (includeDocuments) Documents = Csv(a.Documents);
            UpdatedAt = DateTime.Now;
        }

        public static string? Csv(IEnumerable<string> values)
        {
            var s = string.Join(",", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
            return s.Length == 0 ? null : s;
        }

        public static List<string> Split(string? csv) =>
            string.IsNullOrWhiteSpace(csv) ? new List<string>() : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>إجابات البوصلة (الخطوات الثلاث): نموذج الصفحة، وكوكي الزائر، والمصدر الوحيد للمطابقة (CompassMatcher).</summary>
    public class CompassAnswers
    {
        // 1 — من أنت
        public string? Residence { get; set; }
        public string? Nationality { get; set; }
        public int? BirthYear { get; set; }
        public string? Gender { get; set; }
        // 2 — مؤهلاتك
        public string? Degree { get; set; }
        public List<string> Fields { get; set; } = new();
        public int? GraduationYear { get; set; }
        public string? University { get; set; }
        public List<string> English { get; set; } = new();
        public string? EnglishScore { get; set; }
        public bool French { get; set; }
        public int? ExperienceYears { get; set; }
        // 3 — ماذا تريد
        public List<string> Goals { get; set; } = new();
        public List<string> HostCountries { get; set; } = new();
        public bool FullyFundedOnly { get; set; }
        public bool RemoteOnly { get; set; }
        public int? StartWithinMonths { get; set; }
        // جاهزيتك (للأعضاء)
        public List<string> Documents { get; set; } = new();

        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Residence) && string.IsNullOrEmpty(Nationality) && string.IsNullOrEmpty(Degree) && Goals.Count == 0;

        /// <summary>يختصر الملف: 0–100 من 8 أسئلة.</summary>
        [JsonIgnore]
        public int CompletionPercent
        {
            get
            {
                var answered = new[]
                {
                    Residence != null, Nationality != null, BirthYear != null, Degree != null,
                    Fields.Count > 0, English.Count > 0 || French, Goals.Count > 0, HostCountries.Count > 0 || FullyFundedOnly || RemoteOnly
                }.Count(x => x);
                return answered * 100 / 8;
            }
        }

        public bool HasIelts => English.Contains("ielts", StringComparer.OrdinalIgnoreCase);
        public bool HasToefl => English.Contains("toefl", StringComparer.OrdinalIgnoreCase);

        /// <summary>أنواع الفرص المستهدفة (من Goals).</summary>
        public List<OpportunityType> GoalTypes() =>
            Goals.Select(g => Enum.TryParse<OpportunityType>(g, true, out var t) ? t : (OpportunityType?)null).Where(t => t != null).Select(t => t!.Value).Distinct().ToList();

        /// <summary>نسخة مطبَّعة: رموز ISO بأحرف كبيرة، قيم معروفة فقط، نطاقات معقولة، بلا تكرار.</summary>
        public CompassAnswers Normalized()
        {
            var year = DateTime.Now.Year;
            static string? Iso(string? v) => FacetParser.NormalizeCode(v);
            static List<string> Keys(IEnumerable<string> v, int max) =>
                v.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length <= 40).Distinct().Take(max).ToList();
            return new CompassAnswers
            {
                Residence = Iso(Residence),
                Nationality = Iso(Nationality),
                BirthYear = BirthYear is >= 1940 && BirthYear <= year - 10 ? BirthYear : null,
                Gender = Gender is "f" or "m" ? Gender : null,
                Degree = CompassMatcher.ParseDegree(Degree)?.ToString(),
                Fields = Keys(Fields, 10),
                GraduationYear = GraduationYear is >= 1960 && GraduationYear <= year + 8 ? GraduationYear : null,
                University = string.IsNullOrWhiteSpace(University) ? null : University.Trim()[..Math.Min(150, University.Trim().Length)],
                English = Keys(English, 2).Where(k => k is "ielts" or "toefl").ToList(),
                EnglishScore = string.IsNullOrWhiteSpace(EnglishScore) ? null : EnglishScore.Trim()[..Math.Min(20, EnglishScore.Trim().Length)],
                French = French,
                ExperienceYears = ExperienceYears is >= 0 and <= 50 ? ExperienceYears : null,
                Goals = Goals.Select(g => Enum.TryParse<OpportunityType>(g?.Trim(), true, out var t) ? t.ToString() : null).Where(g => g != null).Distinct().Select(g => g!).ToList(),
                HostCountries = HostCountries.Select(Iso).Where(c => c != null).Distinct().Take(10).Select(c => c!).ToList(),
                FullyFundedOnly = FullyFundedOnly,
                RemoteOnly = RemoteOnly,
                StartWithinMonths = StartWithinMonths is 0 or 3 or 6 or 12 ? StartWithinMonths : null,
                Documents = Keys(Documents, 20)
            };
        }
    }

    /// <summary>كوكي الزائر fk_compass: JSON بأسماء camelCase دون القيم الافتراضية، بترميز base64url، 30 يومًا.</summary>
    public static class CompassCookie
    {
        public const string Name = "fk_compass";
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        public static string Serialize(CompassAnswers answers) => TokenCodec.Encode(JsonSerializer.Serialize(answers.Normalized(), Options));

        public static CompassAnswers? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 4000) return null;
            try
            {
                var json = TokenCodec.Decode(value);
                return json == null ? null : JsonSerializer.Deserialize<CompassAnswers>(json, Options)?.Normalized();
            }
            catch (JsonException) { return null; }
            catch (FormatException) { return null; }
        }

        public static CompassAnswers? Read(HttpRequest request) => Parse(request.Cookies[Name]);

        public static void Write(HttpResponse response, CompassAnswers answers) =>
            response.Cookies.Append(Name, Serialize(answers),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax });

        public static void Delete(HttpResponse response) => response.Cookies.Delete(Name);
    }
}
