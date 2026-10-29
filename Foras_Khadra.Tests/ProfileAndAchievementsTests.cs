using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>المرحلة 4: اكتمال الملف (4-4) وشارات الإنجاز (4-2) — منطق صافٍ بلا قاعدة.</summary>
    public class ProfileAndAchievementsTests
    {
        [Fact]
        public void ProfileCompletion_EmptyUser_IsZero_AndAsksForNationalityFirst()
        {
            var r = ProfileCompletion.Compute(new ApplicationUser());
            Assert.Equal(0, r.Percent);
            Assert.False(r.IsComplete);
            Assert.Equal("nationality", r.NextKey);
            Assert.Equal(5, r.MissingKeys.Count);
        }

        [Fact]
        public void ProfileCompletion_CountsEachFieldEqually()
        {
            var u = new ApplicationUser { FirstName = "سارة", LastName = "عضو", Nationality = "الأردن" };
            var r = ProfileCompletion.Compute(u);
            Assert.Equal(60, r.Percent);
            Assert.Equal(new[] { "interests", "country" }, r.MissingKeys);
            Assert.Equal("interests", r.NextKey);
        }

        [Fact]
        public void ProfileCompletion_FullUser_IsComplete()
        {
            var u = new ApplicationUser { FirstName = "a", LastName = "b", Nationality = "مصر", Country = "مصر", Interests = new List<string> { "jobs" } };
            var r = ProfileCompletion.Compute(u);
            Assert.True(r.IsComplete);
            Assert.Null(r.NextKey);
        }

        [Fact]
        public void ProfileCompletion_PromptsExistInAllLanguages()
        {
            foreach (var key in new[] { "nationality", "interests", "country", "first_name", "last_name" })
                foreach (var lang in new[] { "ar", "en", "fr" })
                    Assert.NotEqual(key, ProfileCompletion.Prompt(key, lang));
        }

        [Fact]
        public void Achievements_NewUser_HasNothingEarned_ButSeesProgress()
        {
            var list = Achievements.Compute(new TrackerStats(0, 0, 0), 40);
            Assert.Equal(7, list.Count);
            Assert.All(list, a => Assert.False(a.Earned));
            Assert.Equal(40, list.Single(a => a.Key == "profile_complete").Percent);
            Assert.Equal(0, list.Single(a => a.Key == "active_seeker").Percent);
        }

        [Fact]
        public void Achievements_EarnedByThresholds()
        {
            var list = Achievements.Compute(new TrackerStats(Saved: 5, Applied: 2, Accepted: 1), 100, referrals: 3);
            var earned = list.Where(a => a.Earned).Select(a => a.Key).ToList();
            Assert.Equal(new[] { "first_save", "profile_complete", "first_apply", "active_seeker", "ambassador", "accepted" }, earned);
            var persistent = list.Single(a => a.Key == "persistent");
            Assert.False(persistent.Earned);
            Assert.Equal(40, persistent.Percent); // 2 من 5
        }

        [Fact]
        public void Achievements_TitlesAndHintsExistInAllLanguages()
        {
            foreach (var a in Achievements.Compute(new TrackerStats(0, 0, 0), 0))
                foreach (var lang in new[] { "ar", "en", "fr" })
                {
                    Assert.NotEqual(a.Key, Achievements.Title(a.Key, lang));
                    Assert.NotEqual(a.Key, Achievements.Hint(a.Key, lang));
                }
        }
    }
}
