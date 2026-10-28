namespace Foras_Khadra.Models
{
    /// <summary>صفحة "نشاطاتنا" العامة: القادم أولًا (الأقرب فالأبعد) ثم السابق (الأحدث فالأقدم).</summary>
    public class ActivitiesPageViewModel
    {
        public List<Activity> Upcoming { get; set; } = new();
        public List<Activity> Past { get; set; } = new();
    }
}
