namespace Foras_Khadra.Services
{
    public class ImageUploadService : IImageUploadService
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

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

            subfolder = subfolder.Trim('/', '\\').Replace('\\', '/');
            var folder = Path.Combine(_env.WebRootPath, subfolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext == ".jpeg") ext = ".jpg";
            var fileName = Guid.NewGuid().ToString("N") + ext;

            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return "/" + subfolder + "/" + fileName;
        }

        private static bool LooksLikeImage(IFormFile file, string ext)
        {
            Span<byte> header = stackalloc byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.Read(header);
            }
            if (read < 4) return false;

            switch (ext.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
                case ".png":
                    return read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                        && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
                case ".gif":
                    return read >= 6 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8'
                        && (header[4] == '7' || header[4] == '9') && header[5] == 'a';
                case ".webp":
                    return read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                        && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
                default:
                    return false;
            }
        }
    }
}
