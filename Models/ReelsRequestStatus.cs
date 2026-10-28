namespace Foras_Khadra.Models
{
    /// <summary>
    /// حالة طلب الريلز. تُشتق من الأعلام الثلاثة المخزّنة في الجدول (IsInProgress/IsCompleted/IsRejected)
    /// حتى لا يحتاج التحويل إلى ترحيل قاعدة بيانات.
    /// </summary>
    public enum ReelsRequestStatus
    {
        /// <summary>طلب جديد لم يحدد الأدمن حالته بعد.</summary>
        Pending = 0,
        /// <summary>الأدمن يعمل عليه ("جاري العمل عليها").</summary>
        InProgress = 1,
        Completed = 2,
        Rejected = 3
    }
}
