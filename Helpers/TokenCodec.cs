using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Foras_Khadra.Helpers
{
    /// <summary>رموز Identity (تأكيد البريد، تغييره، الاستعادة) تحوي + و/ و= فتُكسر في الروابط؛ تُمرَّر base64url.</summary>
    public static class TokenCodec
    {
        public static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        /// <summary>null إن لم يكن النص base64url صالحًا.</summary>
        public static string? Decode(string? encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded)) return null;
            try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded.Trim())); }
            catch (FormatException) { return null; }
        }
    }
}
