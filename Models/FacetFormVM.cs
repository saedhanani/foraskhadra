using System.ComponentModel.DataAnnotations;
using Foras_Khadra.Helpers;

namespace Foras_Khadra.Models
{
    /// <summary>حقول الأهلية المهيكلة في نماذج الإنشاء/التعديل والتعبئة السريعة (Views/Shared/_FacetFields.cshtml).</summary>
    public class FacetFormVM
    {
        public FundingType FundingType { get; set; }
        public List<DegreeLevels> DegreeLevels { get; set; } = new();
        public List<int> FieldIds { get; set; } = new();
        public List<GreenThemes> GreenThemes { get; set; } = new();

        [StringLength(2)] public string? HostCountryIso { get; set; }
        public bool IsRemote { get; set; }
        public bool IsOnline { get; set; }
        [Range(10, 99)] public int? MinAge { get; set; }
        [Range(10, 99)] public int? MaxAge { get; set; }
        public LanguageRequirement LanguageRequirement { get; set; }
        [StringLength(100)] public string? LanguageScore { get; set; }
        public bool HasApplicationFee { get; set; }
        public bool ForWomenOnly { get; set; }
        [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
        [StringLength(200)] public string? Venue { get; set; }
        [StringLength(200)] public string? HostOrganizationName { get; set; }
        [HttpUrl] public string? HostOrganizationUrl { get; set; }
        public EmploymentType EmploymentType { get; set; }
        [StringLength(60), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "حروف إنجليزية صغيرة وأرقام وشرطات فقط، مثل daad-epos")]
        public string? ProgramKey { get; set; }

        public static FacetFormVM From(Opportunity o) => new()
        {
            FundingType = o.FundingType,
            DegreeLevels = Enum.GetValues<DegreeLevels>()
                .Where(v => v != Models.DegreeLevels.None && v != Models.DegreeLevels.Any && o.DegreeLevels.HasFlag(v)).ToList(),
            FieldIds = o.Fields?.Select(f => f.Id).ToList() ?? new List<int>(),
            GreenThemes = Enum.GetValues<GreenThemes>().Where(v => v != Models.GreenThemes.None && o.GreenThemes.HasFlag(v)).ToList(),
            HostCountryIso = o.HostCountryIso,
            IsRemote = o.IsRemote,
            IsOnline = o.IsOnline,
            MinAge = o.MinAge,
            MaxAge = o.MaxAge,
            LanguageRequirement = o.LanguageRequirement,
            LanguageScore = o.LanguageScore,
            HasApplicationFee = o.HasApplicationFee,
            ForWomenOnly = o.ForWomenOnly,
            StartDate = o.StartDate,
            Venue = o.Venue,
            HostOrganizationName = o.HostOrganizationName,
            HostOrganizationUrl = o.HostOrganizationUrl,
            EmploymentType = o.EmploymentType,
            ProgramKey = o.ProgramKey,
        };

        /// <summary>التحقق قبل الحفظ (يُستدعى قبل ModelState.IsValid). يعيد رسالة خطأ أو null.</summary>
        public string? Validate()
        {
            var host = FacetParser.NormalizeCode(HostCountryIso);
            if (!string.IsNullOrWhiteSpace(HostCountryIso) && (host == null || !FacetParser.IsRealIso(host)))
                return "رمز بلد الاستضافة يجب أن يكون حرفين (ISO) مثل DE أو TR";
            if (MinAge.HasValue && MaxAge.HasValue && MinAge > MaxAge) return "الحد الأدنى للعمر أكبر من الأقصى";
            if (!string.IsNullOrWhiteSpace(HostOrganizationUrl) && HttpUrl.Normalize(HostOrganizationUrl) == null) return "رابط الجهة المضيفة غير صالح";
            return null;
        }

        /// <summary>ينسخ القيم إلى الفرصة. يعيد رسالة خطأ أو null.</summary>
        public string? ApplyTo(Opportunity o, IEnumerable<Field> allFields)
        {
            if (Validate() is string error) return error;
            var host = FacetParser.NormalizeCode(HostCountryIso);
            var hostUrl = string.IsNullOrWhiteSpace(HostOrganizationUrl) ? null : HttpUrl.Normalize(HostOrganizationUrl);

            o.FundingType = FundingType;
            o.DegreeLevels = DegreeLevels.Aggregate(Models.DegreeLevels.None, (acc, v) => acc | v);
            o.GreenThemes = GreenThemes.Aggregate(Models.GreenThemes.None, (acc, v) => acc | v);
            o.Fields = allFields.Where(f => FieldIds.Contains(f.Id)).ToList();
            o.HostCountryIso = host;
            o.IsRemote = IsRemote;
            o.IsOnline = IsOnline;
            o.MinAge = MinAge;
            o.MaxAge = MaxAge;
            o.LanguageRequirement = LanguageRequirement;
            o.LanguageScore = string.IsNullOrWhiteSpace(LanguageScore) ? null : LanguageScore.Trim();
            o.HasApplicationFee = HasApplicationFee;
            o.ForWomenOnly = ForWomenOnly;
            o.StartDate = StartDate;
            o.Venue = string.IsNullOrWhiteSpace(Venue) ? null : Venue.Trim();
            o.HostOrganizationName = string.IsNullOrWhiteSpace(HostOrganizationName) ? null : HostOrganizationName.Trim();
            o.HostOrganizationUrl = hostUrl;
            o.EmploymentType = EmploymentType;
            o.ProgramKey = string.IsNullOrWhiteSpace(ProgramKey) ? null : ProgramKey.Trim().ToLowerInvariant();
            return null;
        }
    }
}
