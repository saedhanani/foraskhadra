using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foras_Khadra.Models
{
    public class Opportunity
    {
        public int Id { get; set; }

        [Required]
        public string TitleAr { get; set; }

        [Required]
        public string TitleEn { get; set; }

        [Required]
        public string TitleFr { get; set; }

        public string ImagePath { get; set; } // اختياري بعد الإنشاء

        public DateTime PublishDate { get; set; } = DateTime.Now;

        public string PublishedBy { get; set; }

        public OpportunityType? Type { get; set; }

        [Required]
        public string DescriptionAr { get; set; }

        [Required]
        public string DescriptionEn { get; set; }
        [Required]
        public string DescriptionFr { get; set; }


        [Required]
        public string DetailsAr { get; set; }


        [Required]
        public string DetailsEn { get; set; }


        [Required]
        public string DetailsFr { get; set; }

        [Required]
        public virtual ICollection<Country> AvailableCountries { get; set; }

        [Required]
        public string EligibilityCriteriaAr { get; set; }

        [Required]
        public string EligibilityCriteriaEn { get; set; }
        [Required]
        public string EligibilityCriteriaFr { get; set; }
        [Required]
        public string BenefitsAr { get; set; }
        [Required]
        public string BenefitsEn { get; set; }
        [Required]
        public string BenefitsFr { get; set; }
        [Required]
        public string ApplyLink { get; set; }

        public DeadlineType? DeadlineType { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        public string CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public ApplicationUser CreatedByUser { get; set; }

        [NotMapped]
        public bool IsPublishedByOrganization { get; set; }

        [NotMapped]
        public bool IsPublishedByAdmin { get; set; }

        [NotMapped]
        public bool IsExpired { get; set; }

        /// <summary>موعد التقديم محدد ولم ينتهِ بعد لكنه خلال 7 أيام — تعرض البطاقات شارة "ينتهي قريبًا".</summary>
        [NotMapped]
        public bool IsDeadlineSoon =>
            DeadlineType == Foras_Khadra.Models.DeadlineType.SpecificDate
            && EndDate.HasValue
            && EndDate.Value.Date >= DateTime.Now.Date
            && EndDate.Value.Date <= DateTime.Now.Date.AddDays(7);
        public virtual ICollection<ReelsRequest> ReelsRequests { get; set; }

        // ===== منصة الفريق (team.foraskhadra.com) =====
        /// <summary>Published للفرص العادية؛ Draft لما تنشئه منصة الفريق بانتظار التدقيق.</summary>
        public OpportunityStatus Status { get; set; } = OpportunityStatus.Published;
        /// <summary>سبب الرفض من المدير (Admin/Review) — يظهر للمنظمة؛ يُمسح عند الاعتماد أو إعادة التقديم.</summary>
        public string? ReviewNote { get; set; }
        /// <summary>وقت آخر اعتماد أو رفض.</summary>
        public DateTime? ReviewedAt { get; set; }
        /// <summary>الرابط الذي رُصدت منه الفرصة (قد يختلف عن رابط التقديم).</summary>
        public string? SourceUrl { get; set; }
        /// <summary>رقم الفرصة في مسار الفرص على منصة الفريق.</summary>
        public int? PipeId { get; set; }
        /// <summary>كُتب نصها آليًا ثم دققه بشر.</summary>
        public bool AiGenerated { get; set; }

        /// <summary>نص البحث المطبّع (كل الحقول بالثلاث لغات) — يملؤه ApplicationDbContext عند الحفظ؛ انظر Helpers/SearchIndexer.</summary>
        public string? SearchText { get; set; }

        // ===== حقول الأهلية المهيكلة (المرحلة 0.5 من خطة 2026؛ العقد مع منصة الكتابة في docs/PIPELINE-PROMPT-v2.md) =====
        public FundingType FundingType { get; set; }
        public DegreeLevels DegreeLevels { get; set; }
        public GreenThemes GreenThemes { get; set; }
        public virtual ICollection<Field> Fields { get; set; } = new List<Field>();
        /// <summary>رموز ISO-2 لدول مؤهَّلة ليست في جدول الدول (غير عربية)، مفصولة بفاصلة. الدول العربية وXA/XW في AvailableCountries.</summary>
        [StringLength(400)] public string? EligibleCountriesIso { get; set; }
        /// <summary>بلد الدراسة/العمل/الحدث (ISO-2) — غير دول الأهلية.</summary>
        [StringLength(2)] public string? HostCountryIso { get; set; }
        public bool IsRemote { get; set; }
        public bool IsOnline { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public LanguageRequirement LanguageRequirement { get; set; }
        [StringLength(100)] public string? LanguageScore { get; set; }
        public bool HasApplicationFee { get; set; }
        public bool ForWomenOnly { get; set; }
        /// <summary>بدء الحدث (مؤتمر/مسابقة) — غير الموعد النهائي للتقديم (EndDate).</summary>
        public DateTime? StartDate { get; set; }
        [StringLength(200)] public string? Venue { get; set; }
        /// <summary>الجهة التي تقدّم الفرصة فعلًا (لـ JobPosting.hiringOrganization) — لا الموقع الناشر.</summary>
        [StringLength(200)] public string? HostOrganizationName { get; set; }
        public string? HostOrganizationUrl { get; set; }
        public string? HostOrganizationLogoUrl { get; set; }
        public EmploymentType EmploymentType { get; set; }
        /// <summary>مفتاح ثابت للبرنامج المتكرر سنويًا (daad-epos) — يربط الدورات ويغذّي «يفتح عادةً في…».</summary>
        [StringLength(60)] public string? ProgramKey { get; set; }
        /// <summary>{"ar":[...],"en":[...],"fr":[...]} عبارات قصيرة للبطاقة.</summary>
        public string? BenefitsSummaryJson { get; set; }
        /// <summary>آخر تحقّق من المصدر (منصة الكتابة أو المدقق) — شارة «تم التحقق بتاريخ».</summary>
        public DateTime? LastCheckedAt { get; set; }
        /// <summary>آخر تعديل (يملؤه ApplicationDbContext عند الحفظ) — lastmod في خريطة الموقع.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>فرصة مموَّلة/مُبرَزة بمقابل (المرحلة 4.5): تظهر أولًا في قوائمها بعلامة «إعلان مموَّل» واضحة. يضبطها المدير فقط (ليست من منصة الكتابة).</summary>
        public bool IsSponsored { get; set; }
        /// <summary>نهاية الإبراز؛ null = حتى يلغيه المدير.</summary>
        public DateTime? SponsoredUntil { get; set; }

        [NotMapped]
        public bool IsSponsoredNow => IsSponsored && (SponsoredUntil == null || SponsoredUntil.Value.Date >= DateTime.Now.Date);

        [NotMapped]
        public string AvailableCountriesNames
        {
            get
            {
                var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                if (AvailableCountries == null || !AvailableCountries.Any())
                    return string.Empty;

                return string.Join(", ", AvailableCountries.Select(c => lang switch
                {
                    "en" => c.NameEn,
                    "fr" => c.NameFr,
                    _ => c.NameAr
                }));
            }
        }

        

    }
}
