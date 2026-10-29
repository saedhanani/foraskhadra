namespace Foras_Khadra.Models
{
    public class HomeViewModel
    {
        public List<Article> LatestArticles { get; set; }
        public List<Opportunity> LatestOpportunities { get; set; }
        public List<Organization> Organizations { get; set; }

        /// <summary>محفوظات الفرد المسجّل لشريط "فرصي" (null للزائر والمنظمات والإدارة).</summary>
        public MyOpportunitiesViewModel? My { get; set; }

    }
}
