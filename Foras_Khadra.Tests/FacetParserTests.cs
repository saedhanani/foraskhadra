using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class FacetParserTests
    {
        private static readonly List<Country> Countries = new()
        {
            new Country { Id = 1, NameAr = "كل دول العالم", NameEn = "All World", NameFr = "Monde", IsoCode = "XW" },
            new Country { Id = 2, NameAr = "الوطن العربي", NameEn = "Arab World", NameFr = "Monde arabe", IsoCode = "XA" },
            new Country { Id = 15, NameAr = "مصر", NameEn = "Egypt", NameFr = "Égypte", IsoCode = "EG" },
            new Country { Id = 4, NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie", IsoCode = "JO" },
        };

        private static Opportunity NewOpp() => new() { AvailableCountries = new List<Country>(), Fields = new List<Field>() };

        [Fact]
        public void FullExample_MapsEveryField()
        {
            var o = NewOpp();
            var f = new FacetDto
            {
                FundingType = "FullyFunded",
                DegreeLevels = new() { "Bachelor", "master" },
                Fields = new() { "environment", "Engineering" },
                GreenThemes = new() { "Climate", "Energy" },
                EligibleCountries = new() { "XA", "de", "eg" },
                HostCountry = "DE",
                IsRemote = false, IsOnline = true,
                MinAge = 18, MaxAge = 30,
                LanguageRequirement = "Either", LanguageScore = "IELTS 6.5",
                HasApplicationFee = false, ForWomenOnly = true,
                StartDate = "2027-03-15", Venue = "Bonn",
                HostOrganization = new() { Name = " DAAD ", Url = "www.daad.de" },
                EmploymentType = "INTERN",
                ProgramKey = "DAAD-EPOS",
                BenefitsSummary = new() { ["ar"] = new() { "راتب شهري", "تذكرة" }, ["en"] = new() { "Stipend" }, ["xx"] = new() { "ignored" } },
            };

            var err = FacetParser.Apply(o, f, Countries, FieldSeed.All);

            Assert.Null(err);
            Assert.Equal(FundingType.FullyFunded, o.FundingType);
            Assert.Equal(DegreeLevels.Bachelor | DegreeLevels.Master, o.DegreeLevels);
            Assert.Equal(new[] { "environment", "engineering" }, o.Fields.Select(x => x.Key));
            Assert.Equal(GreenThemes.Climate | GreenThemes.Energy, o.GreenThemes);
            Assert.Equal(new[] { 2, 15 }, o.AvailableCountries.Select(c => c.Id)); // XA → الوطن العربي، EG → مصر
            Assert.Equal("DE", o.EligibleCountriesIso);                           // ألمانيا ليست في جدول الدول → نصيًا
            Assert.Equal("DE", o.HostCountryIso);
            Assert.False(o.IsRemote); Assert.True(o.IsOnline);
            Assert.Equal(18, o.MinAge); Assert.Equal(30, o.MaxAge);
            Assert.Equal(LanguageRequirement.Either, o.LanguageRequirement);
            Assert.Equal("IELTS 6.5", o.LanguageScore);
            Assert.False(o.HasApplicationFee); Assert.True(o.ForWomenOnly);
            Assert.Equal(new DateTime(2027, 3, 15), o.StartDate);
            Assert.Equal("Bonn", o.Venue);
            Assert.Equal("DAAD", o.HostOrganizationName);
            Assert.Equal("https://www.daad.de", o.HostOrganizationUrl);
            Assert.Equal(EmploymentType.Intern, o.EmploymentType);
            Assert.Equal("daad-epos", o.ProgramKey);
            Assert.Contains("راتب شهري", o.BenefitsSummaryJson);
            Assert.DoesNotContain("ignored", o.BenefitsSummaryJson);
            Assert.Equal(new[] { "XA", "EG", "DE" }, FacetParser.EligibleCodes(o));
        }

        [Fact]
        public void MissingFields_LeaveExistingValuesUntouched()
        {
            var o = NewOpp();
            o.FundingType = FundingType.Paid; o.ProgramKey = "keep"; o.MinAge = 20;
            var err = FacetParser.Apply(o, new FacetDto { IsRemote = true }, Countries, FieldSeed.All);
            Assert.Null(err);
            Assert.Equal(FundingType.Paid, o.FundingType);
            Assert.Equal("keep", o.ProgramKey);
            Assert.Equal(20, o.MinAge);
            Assert.True(o.IsRemote);
        }

        [Fact]
        public void AnyDegree_ExpandsToAllLevels()
        {
            var o = NewOpp();
            Assert.Null(FacetParser.Apply(o, new FacetDto { DegreeLevels = new() { "Any" } }, Countries, FieldSeed.All));
            Assert.Equal(DegreeLevels.Any, o.DegreeLevels);
            Assert.True(o.DegreeLevels.HasFlag(DegreeLevels.PhD));
        }

        [Theory]
        [InlineData("fundingType", "Free", "bad_funding")]
        [InlineData("degree", "Kindergarten", "bad_degree")]
        [InlineData("field", "astrology", "bad_field")]
        [InlineData("theme", "Space", "bad_theme")]
        [InlineData("country", "Egypt", "bad_country")]
        [InlineData("country", "ZZ", "bad_country")]
        [InlineData("hostCountry", "XA", "bad_host_country")]
        [InlineData("language", "German", "bad_language")]
        [InlineData("employment", "FREELANCE", "bad_employment")]
        [InlineData("startDate", "15/03/2027", "bad_start_date")]
        [InlineData("programKey", "DAAD EPOS!", "bad_program_key")]
        [InlineData("hostUrl", "javascript:alert(1)", "bad_host_url")]
        public void UnknownValues_ReturnNamedError(string field, string value, string expected)
        {
            var f = field switch
            {
                "fundingType" => new FacetDto { FundingType = value },
                "degree" => new FacetDto { DegreeLevels = new() { value } },
                "field" => new FacetDto { Fields = new() { value } },
                "theme" => new FacetDto { GreenThemes = new() { value } },
                "country" => new FacetDto { EligibleCountries = new() { value } },
                "hostCountry" => new FacetDto { HostCountry = value },
                "language" => new FacetDto { LanguageRequirement = value },
                "employment" => new FacetDto { EmploymentType = value },
                "startDate" => new FacetDto { StartDate = value },
                "programKey" => new FacetDto { ProgramKey = value },
                _ => new FacetDto { HostOrganization = new() { Name = "X", Url = value } },
            };
            var err = FacetParser.Apply(NewOpp(), f, Countries, FieldSeed.All);
            Assert.NotNull(err);
            Assert.Equal(expected, err!.Code);
        }

        [Fact]
        public void EmptyEligibleList_AndBadAges_AreRejected()
        {
            Assert.Equal("bad_country", FacetParser.Apply(NewOpp(), new FacetDto { EligibleCountries = new() }, Countries, FieldSeed.All)!.Code);
            Assert.Equal("bad_age", FacetParser.Apply(NewOpp(), new FacetDto { MinAge = 30, MaxAge = 20 }, Countries, FieldSeed.All)!.Code);
            Assert.Equal("bad_age", FacetParser.Apply(NewOpp(), new FacetDto { MaxAge = 5 }, Countries, FieldSeed.All)!.Code);
        }

        [Fact]
        public void BenefitsSummary_IsTrimmedAndCapped()
        {
            var o = NewOpp();
            var items = Enumerable.Range(1, 10).Select(i => "  item " + i + new string('x', 100)).ToList();
            Assert.Null(FacetParser.Apply(o, new FacetDto { BenefitsSummary = new() { ["ar"] = items } }, Countries, FieldSeed.All));
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(o.BenefitsSummaryJson!)!;
            Assert.Equal(FacetParser.MaxSummaryItems, parsed["ar"].Count);
            Assert.All(parsed["ar"], s => Assert.True(s.Length <= FacetParser.MaxSummaryItemLength && !s.StartsWith(' ')));
        }

        [Fact]
        public void SeedKeysAreUniqueAndIncludeAny()
        {
            Assert.Equal(FieldSeed.All.Length, FieldSeed.All.Select(f => f.Key).Distinct().Count());
            Assert.Contains(FieldSeed.All, f => f.Key == Field.AnyKey);
            Assert.All(FieldSeed.All, f => Assert.Matches("^[a-z0-9-]+$", f.Key));
        }

        [Fact]
        public void EmploymentMapsToJobPostingValues()
        {
            Assert.Equal("VOLUNTEER", FacetParser.ToJobPostingValue(EmploymentType.Volunteer));
            Assert.Equal("FULL_TIME", FacetParser.ToJobPostingValue(EmploymentType.FullTime));
            Assert.Null(FacetParser.ToJobPostingValue(EmploymentType.None));
            Assert.Equal(EmploymentType.PartTime, FacetParser.ParseEmployment("part-time"));
        }
    }
}
