using System.Text;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Http;
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
