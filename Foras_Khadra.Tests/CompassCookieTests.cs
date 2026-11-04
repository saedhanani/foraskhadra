using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>كوكي الزائر fk_compass والتطبيع: ذهاب وإياب بلا فقد، قيم غير معروفة تسقط، مدخل تالف يعيد null.</summary>
    public class CompassCookieTests
    {
        [Fact]
        public void RoundTrip_KeepsAnswers()
        {
            var a = new CompassAnswers
            {
                Residence = "jo", Nationality = "EG", BirthYear = 1998, Gender = "f", Degree = "master",
                Fields = new() { "Energy", "water", "energy" }, GraduationYear = 2021, University = "  JUST ",
                English = new() { "IELTS" }, EnglishScore = "6.5", French = true, ExperienceYears = 3,
                Goals = new() { "scholarships", "Jobs", "bogus" }, HostCountries = new() { "de", "GB" },
                FullyFundedOnly = true, RemoteOnly = false, StartWithinMonths = 3, Documents = new() { "cv" }
            };

            var parsed = CompassCookie.Parse(CompassCookie.Serialize(a));
            Assert.NotNull(parsed);
            Assert.Equal("JO", parsed!.Residence);
            Assert.Equal("EG", parsed.Nationality);
            Assert.Equal(1998, parsed.BirthYear);
            Assert.Equal("f", parsed.Gender);
            Assert.Equal("Master", parsed.Degree);
            Assert.Equal(new[] { "energy", "water" }, parsed.Fields);
            Assert.Equal("JUST", parsed.University);
            Assert.Equal(new[] { "ielts" }, parsed.English);
            Assert.True(parsed.French);
            Assert.Equal(new[] { "Scholarships", "Jobs" }, parsed.Goals);
            Assert.Equal(new[] { "DE", "GB" }, parsed.HostCountries);
            Assert.True(parsed.FullyFundedOnly);
            Assert.Equal(3, parsed.StartWithinMonths);
            Assert.Equal(new[] { "cv" }, parsed.Documents);
        }

        [Fact]
        public void Normalized_DropsOutOfRangeAndUnknownValues()
        {
            var a = new CompassAnswers
            {
                Residence = "Jordan", BirthYear = 1900, Gender = "x", Degree = "Any", English = new() { "cambridge" },
                ExperienceYears = 99, StartWithinMonths = 5, Goals = new() { "" }
            }.Normalized();
            Assert.Null(a.Residence);
            Assert.Null(a.BirthYear);
            Assert.Null(a.Gender);
            Assert.Null(a.Degree);
            Assert.Empty(a.English);
            Assert.Null(a.ExperienceYears);
            Assert.Null(a.StartWithinMonths);
            Assert.Empty(a.Goals);
            Assert.True(a.IsEmpty);
        }

        [Fact]
        public void Parse_RejectsGarbage()
        {
            Assert.Null(CompassCookie.Parse(null));
            Assert.Null(CompassCookie.Parse(""));
            Assert.Null(CompassCookie.Parse("not base64!!"));
            Assert.Null(CompassCookie.Parse(new string('A', 5000)));
        }

        [Fact]
        public void Completion_CountsAnsweredQuestions()
        {
            Assert.Equal(0, new CompassAnswers().CompletionPercent);
            var half = new CompassAnswers { Residence = "JO", Nationality = "JO", BirthYear = 2000, Degree = "Bachelor" };
            Assert.Equal(50, half.CompletionPercent);
            var full = new CompassAnswers { Residence = "JO", Nationality = "JO", BirthYear = 2000, Degree = "Bachelor", Fields = new() { "it" }, French = true, Goals = new() { "Jobs" }, RemoteOnly = true };
            Assert.Equal(100, full.CompletionPercent);
        }
    }
}
