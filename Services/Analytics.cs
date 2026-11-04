using System.Collections.Concurrent;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// تحليلات داخلية بلا كوكيز: مشاهدات الصفحات ومصادر الزيارات والأحداث تُجمَّع في الذاكرة (singleton) وتُكتب كصفوف يومية
    /// كل دقيقة (AnalyticsFlushService). لا عناوين IP ولا معرّفات زوار؛ الزاحفات والمديرون لا يُعدّون.
    /// </summary>
    public interface IAnalytics
    {
        void PageView(string normalizedPath, string culture, string source, int? opportunityId);
        void Event(string name, string? label = null);
    }

    public sealed record AnalyticsSnapshot(
        Dictionary<(DateOnly Day, string Path, string Culture), int> Views,
        Dictionary<(DateOnly Day, string Source), int> Sources,
        Dictionary<(DateOnly Day, string Event, string? Label), int> Events,
        Dictionary<(DateOnly Day, int OpportunityId), int> OpportunityViews)
    {
        public bool IsEmpty => Views.Count == 0 && Sources.Count == 0 && Events.Count == 0 && OpportunityViews.Count == 0;
    }

    public sealed class Analytics : IAnalytics
    {
        /// <summary>سقف المفاتيح المختلفة في الذاكرة بين كتابتين — ضد سيل روابط عشوائية.</summary>
        public const int MaxKeysPerBucket = 5000;

        private ConcurrentDictionary<(DateOnly, string, string), int> _views = new();
        private ConcurrentDictionary<(DateOnly, string), int> _sources = new();
        private ConcurrentDictionary<(DateOnly, string, string?), int> _events = new();
        private ConcurrentDictionary<(DateOnly, int), int> _opportunityViews = new();

        public void PageView(string normalizedPath, string culture, string source, int? opportunityId)
        {
            var day = DateOnly.FromDateTime(DateTime.Now);
            Bump(_views, (day, normalizedPath, CultureRouting.Normalize(culture)));
            if (source != "internal") Bump(_sources, (day, source));
            if (opportunityId is > 0) Bump(_opportunityViews, (day, opportunityId.Value));
        }

        public void Event(string name, string? label = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim().ToLowerInvariant();
            if (name.Length > 60) name = name[..60];
            var cleanLabel = string.IsNullOrWhiteSpace(label) ? null : AnalyticsRules.CleanLabel(label);
            Bump(_events, (DateOnly.FromDateTime(DateTime.Now), name, cleanLabel));
        }

        /// <summary>يأخذ ما تجمّع ويبدأ عدّادات فارغة (يستدعيه المُفرِّغ).</summary>
        public AnalyticsSnapshot Drain()
        {
            var views = Interlocked.Exchange(ref _views, new ConcurrentDictionary<(DateOnly, string, string), int>());
            var sources = Interlocked.Exchange(ref _sources, new ConcurrentDictionary<(DateOnly, string), int>());
            var events = Interlocked.Exchange(ref _events, new ConcurrentDictionary<(DateOnly, string, string?), int>());
            var opp = Interlocked.Exchange(ref _opportunityViews, new ConcurrentDictionary<(DateOnly, int), int>());
            return new AnalyticsSnapshot(
                views.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2, kv.Key.Item3), kv => kv.Value),
                sources.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2), kv => kv.Value),
                events.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2, kv.Key.Item3), kv => kv.Value),
                opp.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2), kv => kv.Value));
        }

        private static void Bump<TKey>(ConcurrentDictionary<TKey, int> bucket, TKey key) where TKey : notnull
        {
            if (bucket.Count >= MaxKeysPerBucket && !bucket.ContainsKey(key)) return;
            bucket.AddOrUpdate(key, 1, (_, n) => n + 1);
        }
    }

    /// <summary>كتابة اللقطة في الجداول اليومية: زيادة الصف إن وُجد وإلا إنشاؤه. مفصول عن الخدمة ليُختبر.</summary>
    public static class AnalyticsFlusher
    {
        public static async Task FlushAsync(ApplicationDbContext db, AnalyticsSnapshot snap, CancellationToken ct = default)
        {
            if (snap.IsEmpty) return;

            var days = snap.Views.Keys.Select(k => k.Day)
                .Concat(snap.Sources.Keys.Select(k => k.Day))
                .Concat(snap.Events.Keys.Select(k => k.Day))
                .Concat(snap.OpportunityViews.Keys.Select(k => k.Day))
                .Distinct().ToList();

            var views = await db.PageViewDailyStats.Where(r => days.Contains(r.Day)).ToListAsync(ct);
            foreach (var (key, n) in snap.Views)
            {
                var row = views.FirstOrDefault(r => r.Day == key.Day && r.Path == key.Path && r.Culture == key.Culture);
                if (row != null) row.Views += n;
                else db.PageViewDailyStats.Add(new PageViewDailyStat { Day = key.Day, Path = key.Path, Culture = key.Culture, Views = n });
            }

            var sources = await db.TrafficSourceDailyStats.Where(r => days.Contains(r.Day)).ToListAsync(ct);
            foreach (var (key, n) in snap.Sources)
            {
                var row = sources.FirstOrDefault(r => r.Day == key.Day && r.Source == key.Source);
                if (row != null) row.Views += n;
                else db.TrafficSourceDailyStats.Add(new TrafficSourceDailyStat { Day = key.Day, Source = key.Source, Views = n });
            }

            var events = await db.SiteEventDailyStats.Where(r => days.Contains(r.Day)).ToListAsync(ct);
            foreach (var (key, n) in snap.Events)
            {
                var row = events.FirstOrDefault(r => r.Day == key.Day && r.Event == key.Event && r.Label == key.Label);
                if (row != null) row.Count += n;
                else db.SiteEventDailyStats.Add(new SiteEventDailyStat { Day = key.Day, Event = key.Event, Label = key.Label, Count = n });
            }

            if (snap.OpportunityViews.Count > 0)
            {
                // صفحات فرص حُذفت بعد المشاهدة لا تُكتب (قيد المفتاح الأجنبي)
                var ids = snap.OpportunityViews.Keys.Select(k => k.OpportunityId).Distinct().ToList();
                var existing = (await db.Opportunities.AsNoTracking().Where(o => ids.Contains(o.Id)).Select(o => o.Id).ToListAsync(ct)).ToHashSet();
                var rows = await db.OpportunityDailyStats.Where(r => ids.Contains(r.OpportunityId) && days.Contains(r.Day)).ToListAsync(ct);
                foreach (var (key, n) in snap.OpportunityViews)
                {
                    if (!existing.Contains(key.OpportunityId)) continue;
                    var row = rows.FirstOrDefault(r => r.OpportunityId == key.OpportunityId && r.Day == key.Day);
                    if (row != null) row.Views += n;
                    else db.OpportunityDailyStats.Add(new OpportunityDailyStat { OpportunityId = key.OpportunityId, Day = key.Day, Views = n });
                }
            }

            await db.SaveChangesAsync(ct);
        }

        /// <summary>الاحتفاظ 400 يومًا (مقارنة سنة بسنة) ثم حذف.</summary>
        public static async Task<int> PruneAsync(ApplicationDbContext db, DateOnly today, CancellationToken ct = default)
        {
            var cutoff = today.AddDays(-400);
            var n = await db.PageViewDailyStats.Where(r => r.Day < cutoff).ExecuteDeleteAsync(ct);
            n += await db.TrafficSourceDailyStats.Where(r => r.Day < cutoff).ExecuteDeleteAsync(ct);
            n += await db.SiteEventDailyStats.Where(r => r.Day < cutoff).ExecuteDeleteAsync(ct);
            return n;
        }
    }

    /// <summary>يفرّغ عدّادات Analytics إلى القاعدة كل دقيقة، وعند إيقاف التطبيق، ويحذف ما تجاوز 400 يوم مرة يوميًا.</summary>
    public sealed class AnalyticsFlushService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(60);

        private readonly Analytics _analytics;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AnalyticsFlushService> _log;
        private DateOnly _prunedOn;

        public AnalyticsFlushService(IAnalytics analytics, IServiceScopeFactory scopes, ILogger<AnalyticsFlushService> log)
        {
            _analytics = (Analytics)analytics;
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(Tick, stoppingToken); }
                catch (OperationCanceledException) { break; }
                await FlushOnceAsync(stoppingToken);
            }
        }

        /// <summary>عند الإيقاف: آخر دقيقة لا تضيع.</summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await FlushOnceAsync(cts.Token);
        }

        private async Task FlushOnceAsync(CancellationToken ct)
        {
            var snap = _analytics.Drain();
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await AnalyticsFlusher.FlushAsync(db, snap, ct);

                var today = DateOnly.FromDateTime(DateTime.Now);
                if (_prunedOn != today)
                {
                    _prunedOn = today;
                    await AnalyticsFlusher.PruneAsync(db, today, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "Analytics flush failed ({Views} paths, {Events} events dropped)", snap.Views.Count, snap.Events.Count);
            }
        }
    }

    public static class AnalyticsMiddlewareExtensions
    {
        /// <summary>
        /// يعدّ مشاهدة بعد اكتمال الرد: GET ناجح لصفحة HTML عامة، ليس زاحفًا ولا مديرًا ولا إعادة تنفيذ 404.
        /// يوضع قبل التوجيه؛ القراءة تتم بعد next() فتكون الهوية معروفة.
        /// </summary>
        public static IApplicationBuilder UseAnalytics(this IApplicationBuilder app)
        {
            var analytics = app.ApplicationServices.GetRequiredService<IAnalytics>();
            return app.Use(async (context, next) =>
            {
                await next();

                var request = context.Request;
                var response = context.Response;
                if (!HttpMethods.IsGet(request.Method) || response.StatusCode != StatusCodes.Status200OK) return;
                if (response.ContentType == null || !response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)) return;
                if (context.Features.Get<IStatusCodeReExecuteFeature>() != null) return;
                if (AnalyticsRules.IsBot(request.Headers.UserAgent)) return;
                if (context.User?.IsInRole("Admin") == true) return;

                var bare = CultureRouting.StripPrefix(request.Path.Value);
                if (!AnalyticsRules.ShouldTrack(bare)) return;

                var (path, opportunityId) = AnalyticsRules.Normalize(request.Path.Value, request.Query);
                var culture = CultureRouting.FromPath(request.Path.Value) ?? CultureRouting.Default;
                analytics.PageView(path, culture, AnalyticsRules.SourceOf(request), opportunityId);
            });
        }
    }
}
