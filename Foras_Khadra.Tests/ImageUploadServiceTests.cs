using System.Text;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class ImageUploadServiceTests
    {
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3 };
        private static readonly byte[] Jpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1 };
        private static readonly byte[] Gif = Encoding.ASCII.GetBytes("GIF89a").Concat(new byte[8]).ToArray();
        private static readonly byte[] Webp = Encoding.ASCII.GetBytes("RIFF").Concat(new byte[4]).Concat(Encoding.ASCII.GetBytes("WEBP")).Concat(new byte[4]).ToArray();
        private static readonly byte[] Html = Encoding.ASCII.GetBytes("<html><script>alert(1)</script></html>");

        // Validate لا يستخدم بيئة الاستضافة، لذا null يكفي هنا
        private readonly ImageUploadService _service = new(null!);

        private static IFormFile File(string name, byte[] bytes)
            => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);

        [Theory]
        [InlineData("a.png", nameof(Png))]
        [InlineData("a.JPG", nameof(Jpg))]
        [InlineData("a.jpeg", nameof(Jpg))]
        [InlineData("a.gif", nameof(Gif))]
        [InlineData("a.webp", nameof(Webp))]
        public void Validate_AcceptsRealImages(string fileName, string bytesField)
        {
            var bytes = (byte[])typeof(ImageUploadServiceTests)
                .GetField(bytesField, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .GetValue(null)!;

            Assert.Null(_service.Validate(File(fileName, bytes)));
        }

        [Theory]
        [InlineData("a.exe")]
        [InlineData("a.html")]
        [InlineData("a.svg")]
        [InlineData("a.aspx")]
        [InlineData("noext")]
        public void Validate_RejectsDisallowedExtensions(string fileName)
        {
            Assert.NotNull(_service.Validate(File(fileName, Png)));
        }

        [Fact]
        public void Validate_RejectsContentThatDoesNotMatchExtension()
        {
            Assert.NotNull(_service.Validate(File("a.png", Html))); // HTML disguised as PNG
            Assert.NotNull(_service.Validate(File("a.jpg", Png)));  // PNG bytes with JPG extension
        }

        [Fact]
        public void Validate_RejectsEmptyNullAndOversized()
        {
            Assert.NotNull(_service.Validate(null));
            Assert.NotNull(_service.Validate(File("a.png", Array.Empty<byte>())));

            var big = new byte[ImageUploadService.MaxBytes + 1];
            Png.CopyTo(big, 0);
            Assert.NotNull(_service.Validate(File("a.png", big)));
        }

        [Fact]
        public async Task SaveAsync_WritesRandomNameUnderSubfolder()
        {
            var root = Path.Combine(Path.GetTempPath(), "fk-upload-tests", Guid.NewGuid().ToString("N"));
            var env = new FakeEnv(root);
            var service = new ImageUploadService(env);

            var path = await service.SaveAsync(File("photo.JPEG", Jpg), "uploads/opportunities");

            Assert.StartsWith("/uploads/opportunities/", path);
            Assert.EndsWith(".jpg", path);
            Assert.DoesNotContain("photo", path);
            Assert.True(System.IO.File.Exists(Path.Combine(root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));

            Directory.Delete(root, recursive: true);
        }

        // ---------------------------------------------------------- ValidateBytes / SaveAsync(bytes): واجهة منصة الفريق

        private static byte[] Encode(SKEncodedImageFormat format, int w = 64, int h = 40)
        {
            using var bmp = new SKBitmap(w, h);
            bmp.Erase(new SKColor(30, 140, 80));
            using var data = bmp.Encode(format, 85);
            return data.ToArray();
        }

        [Theory]
        [InlineData(SKEncodedImageFormat.Jpeg, ".jpg")]
        [InlineData(SKEncodedImageFormat.Png, ".png")]
        [InlineData(SKEncodedImageFormat.Webp, ".webp")]
        public void ValidateBytes_AcceptsRealImages(SKEncodedImageFormat format, string ext)
        {
            var check = _service.ValidateBytes(Encode(format));

            Assert.Null(check.Error);
            Assert.Equal(ext, check.Extension);
            Assert.Equal(64, check.Width);
            Assert.Equal(40, check.Height);
        }

        [Fact]
        public void ValidateBytes_RejectsNonImagesAndHeaderOnlyFakes()
        {
            Assert.Equal("bad_image", _service.ValidateBytes(Array.Empty<byte>()).Error);
            Assert.Equal("bad_image", _service.ValidateBytes(Html).Error);
            Assert.Equal("bad_image", _service.ValidateBytes(Gif).Error);  // GIF غير مسموح في الواجهة
            Assert.Equal("bad_image", _service.ValidateBytes(Jpg).Error);  // توقيع JPEG وحده دون صورة
            Assert.Equal("bad_image", _service.ValidateBytes(Png).Error);
            Assert.Equal("bad_image", _service.ValidateBytes(Webp).Error);
        }

        [Fact]
        public void ValidateBytes_RejectsTruncatedImage()
        {
            var jpg = Encode(SKEncodedImageFormat.Jpeg, 400, 300);
            Assert.Equal("bad_image", _service.ValidateBytes(jpg[..(jpg.Length / 2)]).Error);
        }

        [Fact]
        public void ValidateBytes_RejectsOversizedBytesAndDimensions()
        {
            var big = new byte[ImageUploadService.ApiMaxBytes + 1];
            Encode(SKEncodedImageFormat.Png).CopyTo(big, 0);
            Assert.Equal("too_large", _service.ValidateBytes(big).Error);

            Assert.Equal("too_large", _service.ValidateBytes(PngWithHeader(10_000, 10_000)).Error);
        }

        [Fact]
        public async Task SaveBytes_WritesRandomNameUnderSubfolderAndChecksSignature()
        {
            var root = Path.Combine(Path.GetTempPath(), "fk-upload-tests", Guid.NewGuid().ToString("N"));
            var service = new ImageUploadService(new FakeEnv(root));
            var webp = Encode(SKEncodedImageFormat.Webp);

            var path = await service.SaveAsync(webp, ".webp", "uploads/opportunities");

            Assert.StartsWith("/uploads/opportunities/", path);
            Assert.EndsWith(".webp", path);
            Assert.Equal(webp, System.IO.File.ReadAllBytes(Path.Combine(root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(webp, ".jpg", "uploads/opportunities"));

            Directory.Delete(root, recursive: true);
        }

        /// <summary>PNG صالح البنية بأبعاد معطاة (IHDR) دون بكسلات كافية: يكفي لاختبار حد الأبعاد دون حجز ذاكرتها.</summary>
        private static byte[] PngWithHeader(int w, int h)
        {
            static byte[] Chunk(string type, byte[] body)
            {
                var t = Encoding.ASCII.GetBytes(type);
                var len = BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(body.Length));
                var crc = BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(Crc32(t.Concat(body).ToArray())));
                return len.Concat(t).Concat(body).Concat(crc).ToArray();
            }
            var ihdr = BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(w))
                .Concat(BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(h)))
                .Concat(new byte[] { 8, 2, 0, 0, 0 }).ToArray(); // 8 بت، RGB
            var idat = new MemoryStream();
            using (var z = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                z.Write(new byte[64]);
            return Png[..8].Concat(Chunk("IHDR", ihdr)).Concat(Chunk("IDAT", idat.ToArray())).Concat(Chunk("IEND", Array.Empty<byte>())).ToArray();
        }

        private static uint Crc32(byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in data)
            {
                crc ^= b;
                for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            return ~crc;
        }

        private sealed class FakeEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
        {
            public FakeEnv(string webRoot) { WebRootPath = webRoot; ContentRootPath = webRoot; }
            public string WebRootPath { get; set; }
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ApplicationName { get; set; } = "tests";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
