using System.Text;

namespace Foras_Khadra.Helpers
{
    /// <summary>
    /// يحوّل عنوانًا إلى مقطع رابط مقروء (slug) يُلحق بالمعرّف: /Opportunity/Details/12/منحة-ماجستير
    /// الحروف من أي لغة تبقى كما هي؛ كل ما عداها يصبح شرطة واحدة.
    /// </summary>
    public static class SlugHelper
    {
        public static string Make(string? text, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var sb = new StringBuilder(maxLength);
            bool lastWasDash = true; // يمنع شرطة في البداية

            foreach (var ch in text.Normalize(NormalizationForm.FormKC).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    lastWasDash = false;
                }
                else if (!lastWasDash)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }

                if (sb.Length >= maxLength) break;
            }

            return sb.ToString().Trim('-');
        }
    }
}
