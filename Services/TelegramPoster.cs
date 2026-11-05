using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// قناة تيليجرام آلية (المرحلة 4.3، بقرار المالك): ينشر كل فرصة جديدة منشورة في القناة عبر Bot API.
    /// معطَّل ما لم تُضبط الإعدادات: Telegram:BotToken وTelegram:ChannelId (مثل @foraskhadra أو -100…).
    /// آخر فرصة نُشرت تُحفظ في App_Data/telegram-state.json فلا يتكرر النشر بعد إعادة التشغيل.
    /// </summary>
    public interface ITelegramPoster
    {
        bool Enabled { get; }
        /// <summary>ينشر الفرص المنشورة الأحدث من آخر منشور (حتى 5 في الدفعة الواحدة لتجنّب الإغراق). يعيد عدد المنشور.</summary>
        Task<int> PostNewAsync(CancellationToken ct = default);
    }

    public class TelegramPoster : ITelegramPoster
    {
        public const string HttpClientName = "telegram";
        public const int MaxPerRun = 5;

        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TelegramPoster> _log;

        public TelegramPoster(ApplicationDbContext db, IHttpClientFactory http, IConfiguration config, IWebHostEnvironment env, ILogger<TelegramPoster> log)
        {
            _db = db;
            _http = http;
            _config = config;
            _env = env;
            _log = log;
        }

        private string? Token => _config["Telegram:BotToken"];
        private string? Channel => _config["Telegram:ChannelId"];
        private string SiteUrl => (_config["Site:BaseUrl"] ?? "https://www.foraskhadra.com").TrimEnd('/');
        public bool Enabled => !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(Channel);

        private string StatePath => Path.Combine(_env.ContentRootPath, "App_Data", "telegram-state.json");

        private sealed class State { public int LastOpportunityId { get; set; } public DateTime? LastPostedAt { get; set; } }

        public async Task<int> PostNewAsync(CancellationToken ct = default)
        {
            if (!Enabled) return 0;

            var state = LoadState();
            var today = DateTime.Now.Date;
            // أول تشغيل: لا نغرق القناة بالأرشيف — نبدأ من آخر فرصة حالية
            if (state.LastOpportunityId == 0)
            {
                state.LastOpportunityId = await _db.Opportunities.Where(o => o.Status == OpportunityStatus.Published).Select(o => (int?)o.Id).MaxAsync(ct) ?? 0;
                SaveState(state);
                return 0;
            }

            var fresh = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.Id > state.LastOpportunityId
                    && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today))
                .OrderBy(o => o.Id)
                .Take(MaxPerRun)
                .ToListAsync(ct);
            if (fresh.Count == 0) return 0;

            var client = _http.CreateClient(HttpClientName);
            var posted = 0;
            foreach (var o in fresh)
            {
                var ok = await SendAsync(client, Format(o), ct);
                if (!ok) break; // نعيد المحاولة في الدورة القادمة من الفرصة نفسها
                state.LastOpportunityId = o.Id;
                state.LastPostedAt = DateTime.Now;
                SaveState(state);
                posted++;
            }
            return posted;
        }

        /// <summary>نص المنشور (HTML تيليجرام): النوع، العنوان، الموعد، الأهلية المختصرة، والرابط بثلاث لغات.</summary>
        public string Format(Opportunity o)
        {
            var type = o.Type.HasValue ? OpportunityTypeExtensions.GetDisplayName(o.Type.Value, "ar") : "فرصة";
            var deadline = o.DeadlineType == DeadlineType.SpecificDate && o.EndDate.HasValue
                ? "⏳ الموعد النهائي: " + o.EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : "⏳ مفتوح حتى إشعار آخر";
            var facts = new List<string>();
            if (o.FundingType == FundingType.FullyFunded) facts.Add("ممولة بالكامل");
            if (o.LanguageRequirement == LanguageRequirement.None) facts.Add("بدون شهادة لغة");
            if (o.IsRemote) facts.Add("عن بُعد");
            if (o.ForWomenOnly) facts.Add("للنساء");
            var desc = Plain(o.DescriptionAr);
            if (desc.Length > 220) desc = desc[..217].TrimEnd() + "…";
            var slug = Uri.EscapeDataString(SlugHelper.Make(o.TitleAr));
            var url = $"{SiteUrl}/Opportunity/Details/{o.Id}/{slug}?utm_source=telegram";

            var sb = new System.Text.StringBuilder();
            sb.Append("🟢 <b>").Append(Esc(type)).Append("</b>\n");
            sb.Append("<b>").Append(Esc(o.TitleAr)).Append("</b>\n\n");
            if (!string.IsNullOrEmpty(desc)) sb.Append(Esc(desc)).Append("\n\n");
            if (facts.Count > 0) sb.Append("✅ ").Append(Esc(string.Join(" · ", facts))).Append('\n');
            sb.Append(Esc(deadline)).Append("\n\n");
            sb.Append("🔗 <a href=\"").Append(url).Append("\">التفاصيل والتقديم</a>");
            return sb.ToString();
        }

        private async Task<bool> SendAsync(HttpClient client, string html, CancellationToken ct)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { chat_id = Channel, text = html, parse_mode = "HTML", disable_web_page_preview = false });
                using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync($"https://api.telegram.org/bot{Token}/sendMessage", content, ct);
                if (resp.IsSuccessStatusCode) return true;
                _log.LogWarning("Telegram sendMessage failed: {Status} {Body}", (int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
                return false;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Telegram sendMessage threw");
                return false;
            }
        }

        private State LoadState()
        {
            try
            {
                if (File.Exists(StatePath))
                    return JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State();
            }
            catch (Exception ex) { _log.LogWarning(ex, "telegram-state.json unreadable; starting fresh"); }
            return new State();
        }

        private void SaveState(State state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
        }

        private static string Plain(string? html) =>
            Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html ?? "", "<.*?>", " ")), @"\s+", " ").Trim();

        private static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
