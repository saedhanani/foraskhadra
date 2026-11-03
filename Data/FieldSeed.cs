using Foras_Khadra.Models;

namespace Foras_Khadra.Data
{
    /// <summary>المجالات الثابتة (HasData): المفاتيح هي العقد مع منصة الكتابة — لا تُعاد تسميتها، تُضاف فقط.</summary>
    public static class FieldSeed
    {
        public static readonly Field[] All =
        {
            F(1, "environment", "البيئة والمناخ", "Environment & Climate", "Environnement et climat"),
            F(2, "energy", "الطاقة", "Energy", "Énergie"),
            F(3, "water", "المياه", "Water", "Eau"),
            F(4, "agriculture", "الزراعة والغذاء", "Agriculture & Food", "Agriculture et alimentation"),
            F(5, "engineering", "الهندسة", "Engineering", "Ingénierie"),
            F(6, "architecture", "العمارة والتخطيط العمراني", "Architecture & Urban Planning", "Architecture et urbanisme"),
            F(7, "it", "تقنية المعلومات والبرمجة", "Computing & IT", "Informatique"),
            F(8, "data-ai", "البيانات والذكاء الاصطناعي", "Data & AI", "Données et IA"),
            F(9, "natural-sciences", "العلوم الطبيعية", "Natural Sciences", "Sciences naturelles"),
            F(10, "health", "الصحة والطب", "Health & Medicine", "Santé et médecine"),
            F(11, "business", "الأعمال والإدارة", "Business & Management", "Commerce et gestion"),
            F(12, "economics", "الاقتصاد والتمويل", "Economics & Finance", "Économie et finance"),
            F(13, "entrepreneurship", "ريادة الأعمال والابتكار", "Entrepreneurship & Innovation", "Entrepreneuriat et innovation"),
            F(14, "law", "القانون", "Law", "Droit"),
            F(15, "political-science", "العلوم السياسية والعلاقات الدولية", "Political Science & International Relations", "Sciences politiques et relations internationales"),
            F(16, "development", "التنمية والعمل الإنساني", "Development & Humanitarian Work", "Développement et action humanitaire"),
            F(17, "leadership", "القيادة والمجتمع المدني", "Leadership & Civil Society", "Leadership et société civile"),
            F(18, "education", "التعليم", "Education", "Éducation"),
            F(19, "social-sciences", "العلوم الاجتماعية", "Social Sciences", "Sciences sociales"),
            F(20, "languages", "اللغات والأدب", "Languages & Literature", "Langues et littérature"),
            F(21, "arts", "الفنون والتصميم", "Arts & Design", "Arts et design"),
            F(22, "media", "الإعلام والاتصال", "Media & Communication", "Médias et communication"),
            F(23, "tourism", "السياحة والضيافة", "Tourism & Hospitality", "Tourisme et hôtellerie"),
            F(24, "vocational", "المهن والحرف", "Vocational & Trades", "Métiers et formation professionnelle"),
            F(25, Field.AnyKey, "كل المجالات", "All fields", "Tous les domaines"),
        };

        private static Field F(int id, string key, string ar, string en, string fr) =>
            new() { Id = id, Key = key, NameAr = ar, NameEn = en, NameFr = fr, SortOrder = id };
    }
}
