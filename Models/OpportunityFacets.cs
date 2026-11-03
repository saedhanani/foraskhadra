namespace Foras_Khadra.Models
{
    // حقول الأهلية المهيكلة للفرصة (المرحلة 0.5 من خطة 2026). العقد مع منصة الكتابة في docs/PIPELINE-PROMPT-v2.md.
    // القيم تُخزَّن أرقامًا؛ الأسماء الإنجليزية هي ما تستقبله واجهة المنصة (Helpers/FacetParser).

    /// <summary>نوع التمويل. Unknown = الصفوف القديمة أو لم يذكره المصدر.</summary>
    public enum FundingType
    {
        Unknown = 0,
        FullyFunded = 1,
        PartiallyFunded = 2,
        SelfFunded = 3,
        /// <summary>وظيفة أو تدريب براتب.</summary>
        Paid = 4
    }

    /// <summary>المراحل الدراسية المقبولة (أعلام تُجمع).</summary>
    [Flags]
    public enum DegreeLevels
    {
        None = 0,
        HighSchool = 1,
        Bachelor = 2,
        Master = 4,
        PhD = 8,
        Postdoc = 16,
        Professional = 32,
        Any = HighSchool | Bachelor | Master | PhD | Postdoc | Professional
    }

    /// <summary>المحاور الخضراء التي تخدمها الفرصة (أعلام تُجمع). None = فرصة عامة.</summary>
    [Flags]
    public enum GreenThemes
    {
        None = 0,
        Climate = 1,
        Energy = 2,
        Water = 4,
        Biodiversity = 8,
        CircularEconomy = 16,
        Agriculture = 32,
        SustainableCities = 64,
        Policy = 128
    }

    /// <summary>شهادة اللغة المطلوبة. Unknown = الصفوف القديمة؛ None = المصدر لم يشترط شهادة.</summary>
    public enum LanguageRequirement
    {
        Unknown = 0,
        None = 1,
        IELTS = 2,
        TOEFL = 3,
        Either = 4,
        French = 5,
        Other = 6
    }

    /// <summary>نوع التوظيف (للوظائف والتدريب والتطوع) بقيم Google JobPosting.</summary>
    public enum EmploymentType
    {
        None = 0,
        FullTime = 1,
        PartTime = 2,
        Contractor = 3,
        Temporary = 4,
        Intern = 5,
        Volunteer = 6
    }
}
