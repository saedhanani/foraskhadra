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
    }
}
