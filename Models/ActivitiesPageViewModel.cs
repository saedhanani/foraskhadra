namespace Foras_Khadra.Models
{
    /// <summary>صفحة "نشاطاتنا" العامة: المشاريع الرائدة ثم القادم (الأقرب فالأبعد) ثم السابق (الأحدث فالأقدم).</summary>
    public class ActivitiesPageViewModel
    {
        public List<FlagshipProgram> Programs { get; set; } = new();
        /// <summary>المشروع المختار في الفلتر (null = الكل).</summary>
        public FlagshipProgram? SelectedProgram { get; set; }
        public List<Activity> Upcoming { get; set; } = new();
        public List<Activity> Past { get; set; } = new();
    }
}
