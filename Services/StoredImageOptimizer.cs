using System.Diagnostics;
using Foras_Khadra.Data;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Services
{
    /// <summary>حصيلة جولة واحدة من تحسين الصور المخزّنة.</summary>
    public sealed class StoredImageReport
    {
        public int Scanned { get; set; }
        /// <summary>صُغّرت واستُبدل ملفها.</summary>
        public int Optimized { get; set; }
        /// <summary>صغيرة أصلًا، أو ملفها غير موجود على القرص، أو مسارها خارج uploads/logos.</summary>
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public long BytesBefore { get; set; }
        public long BytesAfter { get; set; }
        /// <summary>false إذا انتهت المهلة قبل المرور على كل الصور: شغّل الأداة مرة أخرى.</summary>
        public bool Finished { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// يمرّ على الصور التي تشير إليها قاعدة البيانات (شعارات المنظمات، صور الفرص والمقالات والفريق والنشاطات والمشاريع)
    /// ويصغّرها بـ ImageOptimizer، ثم يحدّث المسارات ويرحّل الأصل إلى App_Data/image-originals (خارج wwwroot) بدل حذفه.
    /// يعمل ضمن مهلة زمنية لأن الاستضافة تقطع الطلبات الطويلة؛ يُستدعى مرارًا حتى يعيد Finished = true.
    /// </summary>
    public sealed class StoredImageOptimizer
    {
        private const string OriginalsFolder = "image-originals";
        private static readonly string[] OptimizablePrefixes = { "/uploads/", "/logos/" };

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StoredImageOptimizer> _log;

        public StoredImageOptimizer(ApplicationDbContext db, IWebHostEnvironment env, ILogger<StoredImageOptimizer> log)
        {
            _db = db;
            _env = env;
            _log = log;
        }

        private sealed record ImageRef(string Path, int MaxEdge, Action<string> Set);

        public async Task<StoredImageReport> RunAsync(TimeSpan budget, CancellationToken ct = default)
        {
            var report = new StoredImageReport { Finished = true };
            var clock = Stopwatch.StartNew();
            var replaced = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // مسار قديم → جديد (الملف الواحد قد تشير إليه عدة سجلات)
            var written = new List<(string Old, string New)>();

            foreach (var image in await CollectAsync(ct))
            {
                if (clock.Elapsed > budget)
                {
                    report.Finished = false;
                    break;
                }

                report.Scanned++;
                var web = image.Path.Trim();

                if (replaced.TryGetValue(web, out var already))
                {
                    image.Set(already);
                    continue;
                }

                var full = ToFullPath(web);
                if (full == null || !File.Exists(full))
                {
                    report.Skipped++;
                    continue;
                }

                try
                {
                    var bytes = await File.ReadAllBytesAsync(full, ct);
                    var optimized = ImageOptimizer.Optimize(bytes, image.MaxEdge);
                    if (optimized == null)
                    {
                        report.Skipped++;
                        continue;
                    }

                    var newName = Guid.NewGuid().ToString("N") + optimized.Extension;
                    var newFull = Path.Combine(Path.GetDirectoryName(full)!, newName);
                    await File.WriteAllBytesAsync(newFull, optimized.Bytes, ct);

                    var newWeb = web[..(web.LastIndexOf('/') + 1)] + newName;
                    replaced[web] = newWeb;
                    image.Set(newWeb);
                    written.Add((full, newFull));

                    report.Optimized++;
                    report.BytesBefore += bytes.Length;
                    report.BytesAfter += optimized.Bytes.Length;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    report.Failed++;
                    report.Errors.Add($"{web}: {ex.Message}");
                    _log.LogWarning(ex, "Image optimization failed for {Path}", web);
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                // لم تُحدَّث المسارات: الملفات الجديدة بلا مرجع، تُحذف ويبقى كل شيء كما كان
                foreach (var (_, created) in written) TryDelete(created);
                throw;
            }

            // الأصول تُنقل خارج wwwroot حتى لا تُقدَّم بعد اليوم ويبقى الرجوع إليها ممكنًا
            foreach (var (old, _) in written)
            {
                try
                {
                    var relative = Path.GetRelativePath(_env.WebRootPath, old);
                    var target = Path.Combine(_env.ContentRootPath, "App_Data", OriginalsFolder, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Move(old, target, overwrite: true);
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"{old}: {ex.Message}");
                    _log.LogWarning(ex, "Could not move original image {Path}", old);
                }
            }

            return report;
        }

        /// <summary>كل مراجع الصور في قاعدة البيانات مع طريقة تحديث كل منها.</summary>
        private async Task<List<ImageRef>> CollectAsync(CancellationToken ct)
        {
            var refs = new List<ImageRef>();

            foreach (var o in await _db.Organizations.Where(o => o.LogoPath != null && o.LogoPath != "").ToListAsync(ct))
                refs.Add(new ImageRef(o.LogoPath!, ImageOptimizer.LogoMaxEdge, p => o.LogoPath = p));

            foreach (var o in await _db.ManualOrganizations.Where(o => o.LogoPath != null && o.LogoPath != "").ToListAsync(ct))
                refs.Add(new ImageRef(o.LogoPath!, ImageOptimizer.LogoMaxEdge, p => o.LogoPath = p));

            foreach (var o in await _db.Opportunities.Where(o => o.ImagePath != null && o.ImagePath != "").ToListAsync(ct))
                refs.Add(new ImageRef(o.ImagePath, ImageOptimizer.PhotoMaxEdge, p => o.ImagePath = p));

            foreach (var a in await _db.Articles.Where(a => a.ImagePath != null && a.ImagePath != "").ToListAsync(ct))
                refs.Add(new ImageRef(a.ImagePath, ImageOptimizer.PhotoMaxEdge, p => a.ImagePath = p));

            foreach (var m in await _db.TeamMember.Where(m => m.ImagePath != null && m.ImagePath != "").ToListAsync(ct))
                refs.Add(new ImageRef(m.ImagePath, ImageOptimizer.PhotoMaxEdge, p => m.ImagePath = p));

            foreach (var a in await _db.Activities.Where(a => a.ImagePath != null && a.ImagePath != "").ToListAsync(ct))
                refs.Add(new ImageRef(a.ImagePath!, ImageOptimizer.PhotoMaxEdge, p => a.ImagePath = p));

            foreach (var program in await _db.FlagshipPrograms.ToListAsync(ct))
            {
                if (!string.IsNullOrWhiteSpace(program.HeroImagePath))
                    refs.Add(new ImageRef(program.HeroImagePath, ImageOptimizer.PhotoMaxEdge, p => program.HeroImagePath = p));

                var gallery = program.Gallery;
                for (var i = 0; i < gallery.Count; i++)
                {
                    var index = i;
                    refs.Add(new ImageRef(gallery[index], ImageOptimizer.PhotoMaxEdge, p =>
                    {
                        var list = program.Gallery;
                        list[index] = p;
                        program.Gallery = list; // يعيد كتابة JSON
                    }));
                }
            }

            return refs;
        }

        /// <summary>المسار على القرص لمسار ويب تحت uploads أو logos داخل wwwroot، أو null لأي مسار آخر.</summary>
        private string? ToFullPath(string web)
        {
            if (!OptimizablePrefixes.Any(p => web.StartsWith(p, StringComparison.OrdinalIgnoreCase)) || web.Contains(".."))
                return null;

            var root = Path.GetFullPath(_env.WebRootPath);
            var full = Path.GetFullPath(Path.Combine(root, web.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* أفضل جهد */ }
        }
    }
}
