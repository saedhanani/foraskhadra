using System.Collections.Generic;
using System.Linq;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>نموذج الإضافة/التعديل يحدّد كل الدول مع «كل دول العالم» أو «الوطن العربي»؛ العرض يطويها في الاسم الشامل.</summary>
    public class DisplayCountriesTests
    {
        private static readonly Country World = new() { Id = 1, IsoCode = "XW", NameAr = "كل دول العالم", NameEn = "All World", NameFr = "Monde" };
        private static readonly Country ArabWorld = new() { Id = 2, IsoCode = "XA", NameAr = "الوطن العربي", NameEn = "The Arab World", NameFr = "Monde arabe" };
        private static readonly Country Jordan = new() { Id = 4, IsoCode = "JO", NameAr = "الأردن", NameEn = "Jordan", NameFr = "Jordanie" };
        private static readonly Country Egypt = new() { Id = 15, IsoCode = "EG", NameAr = "مصر", NameEn = "Egypt", NameFr = "Égypte" };
        private static readonly Country Germany = new() { Id = 40, IsoCode = "DE", NameAr = "ألمانيا", NameEn = "Germany", NameFr = "Allemagne" };

        private static int[] Ids(params Country[] countries) =>
            new Opportunity { AvailableCountries = countries.ToList() }.DisplayCountries.Select(c => c.Id).ToArray();

        [Fact]
        public void World_hides_every_other_country() =>
            Assert.Equal(new[] { 1 }, Ids(Jordan, ArabWorld, World, Egypt, Germany));

        [Fact]
        public void Arab_world_hides_arab_countries_and_keeps_others() =>
            Assert.Equal(new[] { 2, 40 }, Ids(Jordan, Egypt, ArabWorld, Germany));

        [Fact]
        public void Plain_countries_are_listed_as_saved() =>
            Assert.Equal(new[] { 4, 15 }, Ids(Jordan, Egypt));

        [Fact]
        public void Api_codes_after_world_edit_are_only_XW() =>
            Assert.Equal(new[] { "XW" }, Foras_Khadra.Helpers.FacetParser.EligibleCodes(
                new Opportunity { AvailableCountries = new List<Country> { World, ArabWorld, Jordan, Egypt }, EligibleCountriesIso = "DE,FR" }));

        [Fact]
        public void Api_codes_after_arab_world_edit_keep_non_arab_codes() =>
            Assert.Equal(new[] { "XA", "DE", "FR" }, Foras_Khadra.Helpers.FacetParser.EligibleCodes(
                new Opportunity { AvailableCountries = new List<Country> { Jordan, ArabWorld, Egypt, Germany }, EligibleCountriesIso = "FR" }));

        [Fact]
        public void No_countries_gives_empty_list()
        {
            Assert.Empty(new Opportunity { AvailableCountries = null! }.DisplayCountries);
            Assert.Equal(string.Empty, new Opportunity { AvailableCountries = new List<Country>() }.AvailableCountriesNames);
        }
    }
}
