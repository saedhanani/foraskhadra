using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class ImageOptimizerTests
    {
        /// <summary>صورة بأبعاد معطاة: معتمة أو بشفافية، PNG (كبيرة الحجم حتى تتجاوز حد التحسين).</summary>
        internal static byte[] Png(int w, int h, bool transparent)
        {
            using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, transparent ? SKAlphaType.Unpremul : SKAlphaType.Opaque));
            // تدرّج عشوائي حتى لا ينضغط الملف إلى بايتات قليلة
            var rnd = new Random(7);
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    bmp.SetPixel(x, y, new SKColor((byte)rnd.Next(256), (byte)(x * 255 / w), (byte)(y * 255 / h), transparent ? (byte)128 : (byte)255));
            using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        [Fact]
        public void Optimize_ShrinksLargeOpaqueImageToJpeg()
        {
            var source = Png(2400, 1600, transparent: false);

            var result = ImageOptimizer.Optimize(source, ImageOptimizer.PhotoMaxEdge);

            Assert.NotNull(result);
            Assert.Equal(".jpg", result!.Extension);
            Assert.Equal(1200, result.Width);
            Assert.Equal(800, result.Height);
            Assert.True(result.Bytes.Length < source.Length / 4);
            Assert.Equal(0xFF, result.Bytes[0]); // توقيع JPEG
        }

        [Fact]
        public void Optimize_KeepsTransparencyAsWebp()
        {
            var source = Png(1000, 500, transparent: true);

            var result = ImageOptimizer.Optimize(source, ImageOptimizer.LogoMaxEdge);

            Assert.NotNull(result);
            Assert.Equal(".webp", result!.Extension);
            Assert.Equal(600, result.Width);
            Assert.Equal(300, result.Height);
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(result.Bytes, 0, 4));
        }

        [Fact]
        public void Optimize_LeavesSmallImagesGifsAndGarbageAlone()
        {
            Assert.Null(ImageOptimizer.Optimize(Png(64, 40, false), ImageOptimizer.PhotoMaxEdge));
            Assert.Null(ImageOptimizer.Optimize(System.Text.Encoding.ASCII.GetBytes("GIF89a").Concat(new byte[200]).ToArray(), ImageOptimizer.PhotoMaxEdge));
            Assert.Null(ImageOptimizer.Optimize(new byte[300], ImageOptimizer.PhotoMaxEdge));
        }

        [Fact]
        public void MaxEdgeFor_LogosAreSmallerThanPhotos()
        {
            Assert.Equal(ImageOptimizer.LogoMaxEdge, ImageOptimizer.MaxEdgeFor("logos"));
            Assert.Equal(ImageOptimizer.LogoMaxEdge, ImageOptimizer.MaxEdgeFor("logos/admin"));
            Assert.Equal(ImageOptimizer.PhotoMaxEdge, ImageOptimizer.MaxEdgeFor("uploads/opportunities"));
        }
    }

    /// <summary>الأداة الإدارية: قاعدة SQLite في الذاكرة ومجلد wwwroot مؤقت.</summary>
    public class StoredImageOptimizerTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly string _root = Path.Combine(Path.GetTempPath(), "fk-optimize-tests", Guid.NewGuid().ToString("N"));
        private readonly StoredImageOptimizer _optimizer;

        public StoredImageOptimizerTests()
        {
            _connection.Open();
            _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();

            Directory.CreateDirectory(Path.Combine(_root, "wwwroot", "logos"));
            Directory.CreateDirectory(Path.Combine(_root, "wwwroot", "uploads"));
            File.WriteAllBytes(Path.Combine(_root, "wwwroot", "logos", "big-logo.png"), ImageOptimizerTests.Png(1200, 1200, transparent: true));
            File.WriteAllBytes(Path.Combine(_root, "wwwroot", "uploads", "big-photo.png"), ImageOptimizerTests.Png(2000, 1000, transparent: false));
            File.WriteAllBytes(Path.Combine(_root, "wwwroot", "uploads", "tiny.png"), ImageOptimizerTests.Png(50, 50, transparent: false));

            _optimizer = new StoredImageOptimizer(_db, new FakeEnv(_root), NullLogger<StoredImageOptimizer>.Instance);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }

        [Fact]
        public async Task Run_ShrinksReferencedImagesUpdatesPathsAndKeepsOriginals()
        {
            // الشعار نفسه تشير إليه منظمتان؛ الصورة الكبيرة يشير إليها مقال ومعرض مشروع؛ صورة صغيرة ومسار مفقود يُتركان
            _db.ManualOrganizations.Add(ManualOrg(1, "/logos/big-logo.png"));
            _db.ManualOrganizations.Add(ManualOrg(2, "/logos/big-logo.png"));
            _db.Articles.Add(new Article { Id = 1, TitleAr = "x", TitleEn = "x", TitleFr = "x", ContentAr = "x", ContentEn = "x", ContentFr = "x", Author = "x", ImagePath = "/uploads/big-photo.png" });
            _db.Articles.Add(new Article { Id = 2, TitleAr = "x", TitleEn = "x", TitleFr = "x", ContentAr = "x", ContentEn = "x", ContentFr = "x", Author = "x", ImagePath = "/uploads/tiny.png" });
            _db.Articles.Add(new Article { Id = 3, TitleAr = "x", TitleEn = "x", TitleFr = "x", ContentAr = "x", ContentEn = "x", ContentFr = "x", Author = "x", ImagePath = "/uploads/missing.png" });
            _db.FlagshipPrograms.Add(new FlagshipProgram { Id = 1, Slug = "p", HeroImagePath = "/uploads/big-photo.png", Gallery = new List<string> { "/uploads/big-photo.png", "/uploads/tiny.png" } });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            var report = await _optimizer.RunAsync(TimeSpan.FromMinutes(1));

            Assert.True(report.Finished);
            Assert.Equal(8, report.Scanned);     // منظمتان + 3 مقالات + صورة المشروع + صورتا المعرض
            Assert.Equal(2, report.Optimized);   // ملفان مختلفان فقط
            Assert.Equal(3, report.Skipped);     // الصغيرة (مرتان) والمفقودة
            Assert.Equal(0, report.Failed);
            Assert.Empty(report.Errors);
            Assert.True(report.BytesAfter < report.BytesBefore / 4);

            _db.ChangeTracker.Clear();
            var orgs = await _db.ManualOrganizations.OrderBy(o => o.Id).ToListAsync();
            Assert.StartsWith("/logos/", orgs[0].LogoPath);
            Assert.EndsWith(".webp", orgs[0].LogoPath);
            Assert.Equal(orgs[0].LogoPath, orgs[1].LogoPath);

            var articles = await _db.Articles.OrderBy(a => a.Id).ToListAsync();
            Assert.EndsWith(".jpg", articles[0].ImagePath);
            Assert.Equal("/uploads/tiny.png", articles[1].ImagePath);
            Assert.Equal("/uploads/missing.png", articles[2].ImagePath);

            var program = await _db.FlagshipPrograms.SingleAsync();
            Assert.Equal(articles[0].ImagePath, program.HeroImagePath);
            Assert.Equal(new[] { articles[0].ImagePath, "/uploads/tiny.png" }, program.Gallery);

            // الملفات الجديدة موجودة والأصول انتقلت إلى App_Data/image-originals
            Assert.True(File.Exists(Path.Combine(_root, "wwwroot", orgs[0].LogoPath!.TrimStart('/'))));
            Assert.True(File.Exists(Path.Combine(_root, "wwwroot", articles[0].ImagePath.TrimStart('/'))));
            Assert.False(File.Exists(Path.Combine(_root, "wwwroot", "logos", "big-logo.png")));
            Assert.True(File.Exists(Path.Combine(_root, "App_Data", "image-originals", "logos", "big-logo.png")));
            Assert.True(File.Exists(Path.Combine(_root, "App_Data", "image-originals", "uploads", "big-photo.png")));
            Assert.True(File.Exists(Path.Combine(_root, "wwwroot", "uploads", "tiny.png")));

            // جولة ثانية لا تجد ما تعمله
            var again = await _optimizer.RunAsync(TimeSpan.FromMinutes(1));
            Assert.Equal(0, again.Optimized);
        }

        [Fact]
        public async Task Run_StopsWhenBudgetIsExhaustedAndReportsUnfinished()
        {
            _db.Articles.Add(new Article { Id = 1, TitleAr = "x", TitleEn = "x", TitleFr = "x", ContentAr = "x", ContentEn = "x", ContentFr = "x", Author = "x", ImagePath = "/uploads/big-photo.png" });
            await _db.SaveChangesAsync();

            var report = await _optimizer.RunAsync(TimeSpan.Zero);

            Assert.False(report.Finished);
            Assert.Equal(0, report.Optimized);
            Assert.True(File.Exists(Path.Combine(_root, "wwwroot", "uploads", "big-photo.png")));
        }

        private static ManualOrganization ManualOrg(int id, string logo) => new()
        {
            Id = id, OrganizationName = "x", ContactPersonName = "x", Details = "x", PhoneNumber = "x",
            Email = "x@test.local", Website = "https://example.test", Location = "x", Country = "x", Sector = "x", LogoPath = logo
        };

        private sealed class FakeEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
        {
            public FakeEnv(string root) { ContentRootPath = root; WebRootPath = Path.Combine(root, "wwwroot"); }
            public string WebRootPath { get; set; }
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ApplicationName { get; set; } = "tests";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
