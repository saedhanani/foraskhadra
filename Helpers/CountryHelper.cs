using Nager.Country;
using System.Collections.Generic;
using System.Linq;

namespace Foras_Khadra.Helpers
{
    public class CountryHelper
    {
        /// <summary>اسم الدولة من رمز ISO-2 بلغة الواجهة (Nager.Country)؛ null إن لم يُعرف الرمز.</summary>
        public static string? NameForIso(string? iso, string lang)
        {
            var code = (iso ?? "").Trim().ToUpperInvariant();
            if (code.Length != 2) return null;
            if (lang == "ar" && ArabicNames.TryGetValue(code, out var ar)) return ar;
            try { return new Nager.Country.CountryProvider().GetCountry(code)?.CommonName; } catch { return null; }
        }

        // أسماء عربية لبلدان الاستضافة الشائعة؛ غيرها يُعرض بالاسم الإنجليزي (Nager.Country بلا حزمة الترجمة)
        private static readonly Dictionary<string, string> ArabicNames = new()
        {
            ["DE"] = "ألمانيا", ["TR"] = "تركيا", ["GB"] = "بريطانيا", ["US"] = "الولايات المتحدة", ["CA"] = "كندا", ["FR"] = "فرنسا",
            ["JP"] = "اليابان", ["KR"] = "كوريا الجنوبية", ["CN"] = "الصين", ["NL"] = "هولندا", ["SE"] = "السويد", ["IT"] = "إيطاليا",
            ["ES"] = "إسبانيا", ["AU"] = "أستراليا", ["MY"] = "ماليزيا", ["HU"] = "المجر", ["RU"] = "روسيا", ["CH"] = "سويسرا",
            ["BE"] = "بلجيكا", ["AT"] = "النمسا", ["IE"] = "أيرلندا", ["NZ"] = "نيوزيلندا", ["SG"] = "سنغافورة", ["NO"] = "النرويج",
            ["FI"] = "فنلندا", ["DK"] = "الدنمارك", ["PL"] = "بولندا", ["CZ"] = "التشيك", ["PT"] = "البرتغال", ["IN"] = "الهند",
            ["ID"] = "إندونيسيا", ["TH"] = "تايلاند", ["ZA"] = "جنوب أفريقيا", ["KE"] = "كينيا", ["BR"] = "البرازيل", ["MX"] = "المكسيك",
            ["EG"] = "مصر", ["JO"] = "الأردن", ["MA"] = "المغرب", ["TN"] = "تونس", ["DZ"] = "الجزائر", ["LB"] = "لبنان", ["PS"] = "فلسطين",
            ["SA"] = "السعودية", ["AE"] = "الإمارات", ["QA"] = "قطر", ["KW"] = "الكويت", ["BH"] = "البحرين", ["OM"] = "عُمان", ["IQ"] = "العراق",
            ["SY"] = "سوريا", ["YE"] = "اليمن", ["SD"] = "السودان", ["LY"] = "ليبيا", ["MR"] = "موريتانيا",
        };

        public static List<string> GetAllCountries()
        {
            var provider = new CountryProvider();
            return provider.GetCountries()
                           .Select(c => c.CommonName)
                           .OrderBy(c => c)
                           .ToList();
        }
    }
}
