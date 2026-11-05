namespace Foras_Khadra.Models
{
    /// <summary>صفحة /Admin/Analytics: ملخص المدة المختارة من الجداول اليومية.</summary>
    public class AnalyticsVM
    {
        public int Days { get; set; } = 7;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        public int TotalViews { get; set; }
        public int PreviousTotalViews { get; set; }
        public List<(DateOnly Day, int Views)> PerDay { get; set; } = new();
        public Dictionary<string, int> ByCulture { get; set; } = new();
        public List<(string Path, int Views)> TopPages { get; set; } = new();
        public List<(string Source, int Views)> TopSources { get; set; } = new();
        /// <summary>الأحداث بلا تسمية (قمع التسجيل والتقديم والبحث).</summary>
        public Dictionary<string, int> Events { get; set; } = new();
        public List<(string Term, int Count)> ZeroResultSearches { get; set; } = new();
        public List<(int Id, string Title, int Views, int ApplyClicks)> TopOpportunities { get; set; } = new();
        public int ApplyClicks { get; set; }
        /// <summary>لوحة المؤشرات (المرحلة 5.1): Services/KpiReport.</summary>
        public Foras_Khadra.Services.KpiSnapshot Kpi { get; set; } = Foras_Khadra.Services.KpiSnapshot.Empty(default, default);
    }
}
