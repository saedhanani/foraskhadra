using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>مفتاح الاهتمام (كما يُخزَّن في InterestsJson عند التسجيل) → نوع الفرصة المقابل.
    /// تستخدمه صفحة "صفحتي" وماسح الإشعارات.</summary>
    public static class InterestMap
    {
        public static readonly IReadOnlyDictionary<string, OpportunityType> ToType = new Dictionary<string, OpportunityType>
        {
            ["competitions"] = OpportunityType.Competitions,
            ["conferences"] = OpportunityType.Conferences,
            ["volunteer_opportunities"] = OpportunityType.Volunteering,
            ["jobs"] = OpportunityType.Jobs,
            ["grants"] = OpportunityType.Scholarships,
            ["fellowships"] = OpportunityType.Fellowships,
            ["training_opportunities"] = OpportunityType.Internships
        };

        /// <summary>أنواع الفرص المقابلة لاهتمامات المستخدم (تتجاهل المفاتيح غير المعروفة).</summary>
        public static List<OpportunityType> TypesFor(IEnumerable<string> interestKeys) =>
            interestKeys.Where(ToType.ContainsKey).Select(k => ToType[k]).Distinct().ToList();
    }
}
