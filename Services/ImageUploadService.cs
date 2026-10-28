using SkiaSharp;

namespace Foras_Khadra.Services
{
    public class ImageUploadService : IImageUploadService
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        /// <summary>حد صور واجهة منصة الفريق بعد فك base64.</summary>
        public const long ApiMaxBytes = 10 * 1024 * 1024; // 10 MB

        /// <summary>حد الأبعاد قبل فك البكسلات، حتى لا تستهلك صورة صغيرة الحجم ضخمة الأبعاد ذاكرة الخادم.</summary>
        public const long ApiMaxPixels = 50_000_000;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IWebHostEnvironment _env;

        public ImageUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "لم يتم اختيار ملف";

            if (file.Length > MaxBytes)
                return "حجم الصورة يتجاوز الحد المسموح (5 ميغابايت)";

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return "نوع الملف غير مسموح؛ المسموح: JPG, PNG, WEBP, GIF";

            // التحقق من محتوى الملف (magic bytes) لا من اسمه فقط
            if (!LooksLikeImage(file, ext))
                return "الملف ليس صورة صالحة";

            return null;
        }

        public async Task<string> SaveAsync(IFormFile file, string subfolder)
        {
            var error = Validate(file);
            if (error != null)
                throw new InvalidOperationException(error);

            var (fullPath, webPath) = NewTarget(subfolder, Path.GetExtension(file.FileName));

            using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return webPath;
        }

        public ImageBytesCheck ValidateBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return new ImageBytesCheck("bad_image", Detail: "empty");

            if (bytes.Length > ApiMaxBytes)
                return new ImageBytesCheck("too_large", Detail: $"{bytes.Length} bytes");

            var ext = DetectSignature(bytes);
            if (ext is not (".jpg" or ".png" or ".webp"))
                return new ImageBytesCheck("bad_image", Detail: "signature");

            try
            {
                using var data = SKData.CreateCopy(bytes);
                using var codec = SKCodec.Create(data);
                if (codec == null)
                    return new ImageBytesCheck("bad_image", Detail: "codec");

                // التنسيق الذي يراه المفكك يجب أن يطابق التوقيع
                var expected = ext switch { ".jpg" => SKEncodedImageFormat.Jpeg, ".png" => SKEncodedImageFormat.Png, _ => SKEncodedImageFormat.Webp };
                if (codec.EncodedFormat != expected)
                    return new ImageBytesCheck("bad_image", Detail: $"format {codec.EncodedFormat} vs {ext}");

                var (w, h) = (codec.Info.Width, codec.Info.Height);
                if (w <= 0 || h <= 0)
                    return new ImageBytesCheck("bad_image", Detail: "dimensions");
                if ((long)w * h > ApiMaxPixels)
                    return new ImageBytesCheck("too_large", Detail: $"{w}x{h}");

                // فك الصورة كاملة: يكشف الملفات المبتورة أو التالفة التي يمر رأسها.
                // JPEG يُفك بمقياس مصغّر إن أمكن (كل البيانات تُقرأ، والذاكرة أقل)
                var size = codec.GetScaledDimensions(Math.Min(1f, 2048f / Math.Max(w, h)));
                var decodeInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var bitmap = new SKBitmap(decodeInfo);
                var result = codec.GetPixels(decodeInfo, bitmap.GetPixels());
                if (result != SKCodecResult.Success)
                    return new ImageBytesCheck("bad_image", Detail: "decode " + result);

                return new ImageBytesCheck(null, ext, w, h);
            }
            catch (Exception ex)
            {
                return new ImageBytesCheck("bad_image", Detail: ex.GetType().Name + ": " + ex.Message);
            }
        }

        public async Task<string> SaveAsync(byte[] bytes, string extension, string subfolder)
        {
            extension = extension.ToLowerInvariant();
            if (extension == ".jpeg") extension = ".jpg";
            if (DetectSignature(bytes) != extension)
                throw new InvalidOperationException("الملف ليس صورة صالحة");

            var (fullPath, webPath) = NewTarget(subfolder, extension);

            using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await stream.WriteAsync(bytes);
            }

            return webPath;
        }

        /// <summary>المسار على القرص ومسار الويب لملف جديد باسم عشوائي داخل wwwroot/{subfolder}.</summary>
        private (string FullPath, string WebPath) NewTarget(string subfolder, string ext)
        {
            subfolder = subfolder.Trim('/', '\\').Replace('\\', '/');
            var folder = Path.Combine(_env.WebRootPath, subfolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            ext = ext.ToLowerInvariant();
            if (ext == ".jpeg") ext = ".jpg";
            var fileName = Guid.NewGuid().ToString("N") + ext;

            return (Path.Combine(folder, fileName), "/" + subfolder + "/" + fileName);
        }

        private static bool LooksLikeImage(IFormFile file, string ext)
        {
            Span<byte> header = stackalloc byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.Read(header);
            }

            ext = ext.ToLowerInvariant();
            if (ext == ".jpeg") ext = ".jpg";
            return DetectSignature(header[..read]) == ext;
        }

        /// <summary>الامتداد الذي يدل عليه توقيع أول البايتات (.jpg/.png/.gif/.webp)، أو null.</summary>
        private static string? DetectSignature(ReadOnlySpan<byte> header)
        {
            if (header.Length < 4) return null;
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return ".jpg";
            if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return ".png";
            if (header.Length >= 6 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8'
                && (header[4] == '7' || header[4] == '9') && header[5] == 'a')
                return ".gif";
            if (header.Length >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
                return ".webp";
            return null;
        }
    }
}
