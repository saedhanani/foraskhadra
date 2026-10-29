using Foras_Khadra.Helpers;
using Foras_Khadra.Services;

namespace Foras_Khadra.Models
{
    /// <summary>فرصة محفوظة كما تُعرض في "فرصي": عنوانها بلغة الواجهة، عدّادها التنازلي، وحالة تقديمها.</summary>
    public record MyOpportunityItem(Opportunity Opportunity, string Title, DeadlineInfo Deadline, TrackerCard? Tracker)
    {
        public ApplicationStatus Status => Tracker?.Status ?? ApplicationStatus.NotApplied;
    }

    /// <summary>صفحة "فرصي" (/Opportunity/Saved) وشريط "فرصي" أعلى الرئيسية: المحفوظات مقسّمة حسب ما يحتاجه المستخدم الآن.</summary>
    public class MyOpportunitiesViewModel
    {
        /// <summary>لم يقدّم بعد وينتهي موعدها خلال 7 أيام — الأقرب أولًا.</summary>
        public List<MyOpportunityItem> Urgent { get; set; } = new();
        /// <summary>لم يقدّم بعد وموعدها أبعد أو مفتوح.</summary>
        public List<MyOpportunityItem> Preparing { get; set; } = new();
        /// <summary>قدّم وينتظر الرد.</summary>
        public List<MyOpportunityItem> Applied { get; set; } = new();
        /// <summary>قُبل أو رُفض.</summary>
        public List<MyOpportunityItem> Results { get; set; } = new();
        /// <summary>انتهى موعدها قبل أن يقدّم.</summary>
        public List<MyOpportunityItem> Expired { get; set; } = new();

        public int Total => Urgent.Count + Preparing.Count + Applied.Count + Results.Count + Expired.Count;
        public int AppliedCount => Applied.Count + Results.Count;
        public int AcceptedCount => Results.Count(i => i.Status == ApplicationStatus.Accepted);

        /// <summary>ما يحتاج تقديمًا الآن: العاجل ثم قيد التحضير — لشريط الرئيسية.</summary>
        public IEnumerable<MyOpportunityItem> UpNext => Urgent.Concat(Preparing);

        public static MyOpportunitiesViewModel Build(IEnumerable<Opportunity> saved, IReadOnlyDictionary<int, TrackerCard> cards,
            string lang, DateTime? today = null)
        {
            var model = new MyOpportunitiesViewModel();
            foreach (var opp in saved)
            {
                var title = lang switch { "en" => opp.TitleEn, "fr" => opp.TitleFr, _ => opp.TitleAr };
                var item = new MyOpportunityItem(opp, title, DeadlineInfo.For(opp, lang, today), cards.GetValueOrDefault(opp.Id));

                switch (item.Status)
                {
                    case ApplicationStatus.Accepted or ApplicationStatus.Rejected: model.Results.Add(item); break;
                    case ApplicationStatus.Applied or ApplicationStatus.Pending: model.Applied.Add(item); break;
                    default:
                        if (item.Deadline.IsExpired) model.Expired.Add(item);
                        else if (item.Deadline.IsUrgent) model.Urgent.Add(item);
                        else model.Preparing.Add(item);
                        break;
                }
            }

            // الأقرب موعدًا أولًا؛ المواعيد المفتوحة في آخر "قيد التحضير"
            int ByDays(MyOpportunityItem i) => i.Deadline.DaysLeft ?? int.MaxValue;
            model.Urgent = model.Urgent.OrderBy(ByDays).ToList();
            model.Preparing = model.Preparing.OrderBy(ByDays).ToList();
            model.Applied = model.Applied.OrderBy(ByDays).ToList();
            model.Expired = model.Expired.OrderByDescending(i => i.Opportunity.EndDate).ToList();
            return model;
        }
    }
}
