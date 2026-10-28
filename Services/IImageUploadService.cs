namespace Foras_Khadra.Services
{
    /// <summary>رفع الصور (شعارات، صور الفرص، المقالات، الفريق) مع التحقق من الامتداد والحجم والمحتوى.</summary>
    public interface IImageUploadService
    {
        /// <summary>يتحقق من الملف؛ يعيد رسالة الخطأ أو null إن كان صالحًا.</summary>
        string? Validate(IFormFile? file);

        /// <summary>
        /// يحفظ الصورة داخل wwwroot/{subfolder} باسم عشوائي ويعيد المسار النسبي للويب (مثل /uploads/x.jpg).
        /// يرمي InvalidOperationException إن لم يجتز الملف التحقق.
        /// </summary>
        Task<string> SaveAsync(IFormFile file, string subfolder);

        /// <summary>
        /// يتحقق من بايتات صورة وصلت دون نموذج (واجهة منصة الفريق): JPEG أو PNG أو WEBP فقط،
        /// بالتوقيع في أول البايتات ثم بفتح الصورة فعلًا. الحد ApiMaxBytes.
        /// </summary>
        ImageBytesCheck ValidateBytes(byte[] bytes);

        /// <summary>
        /// كـ SaveAsync(IFormFile) لكن من بايتات اجتازت ValidateBytes: المجلد نفسه واسم عشوائي بالامتداد المعطى.
        /// يرمي InvalidOperationException إن لم يطابق توقيعُ البايتات الامتداد.
        /// </summary>
        Task<string> SaveAsync(byte[] bytes, string extension, string subfolder);
    }

    /// <summary>نتيجة ValidateBytes. Error: null إن كانت صالحة، وإلا "bad_image" أو "too_large"؛ Detail سبب الرفض للسجل.</summary>
    public sealed record ImageBytesCheck(string? Error, string Extension = "", int Width = 0, int Height = 0, string? Detail = null);
}
