namespace Foras_Khadra.Models
{
    /// <summary>
    /// حالة النشر. الصفحات العامة تعرض Published فقط (كل الاستعلامات تقارن بها مباشرة)،
    /// فأي حالة أخرى تُضاف هنا تبقى مخفية عن الزوار تلقائيًا.
    /// </summary>
    public enum OpportunityStatus
    {
        /// <summary>مسودة من منصة الفريق، تُعرض للإدارة فقط حتى يعتمدها المدقق.</summary>
        Draft = 0,
        Published = 1,
        /// <summary>أنشأتها أو عدّلتها منظمة غير موثَّقة؛ بانتظار موافقة المدير (Admin/Review).</summary>
        PendingReview = 2,
        /// <summary>رفضها المدير مع سبب في ReviewNote؛ تعديل المنظمة يعيدها إلى PendingReview.</summary>
        Rejected = 3,
        /// <summary>كانت منشورة وأُخفيت لأن منظمتها موقوفة؛ رفع الإيقاف يعيدها Published.</summary>
        Suspended = 4
    }
}
