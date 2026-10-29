using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// خزانة الأعضاء (البند 5-4): الإدارة (قائمة/إضافة/تعديل/حذف) للأدمن، وصفحة Vault للمسجّلين.
    /// المورد إمّا ملف PDF يُحفظ في App_Data/resources (خارج wwwroot فلا يُقدَّم إلا عبر Download للمسجّلين) أو رابط خارجي.
    /// </summary>
    [Authorize]
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public const long MaxPdfBytes = 10 * 1024 * 1024; // 10 MB

        public ResourcesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================= VAULT (members) — أي مسجّل =================
        [HttpGet]
        public async Task<IActionResult> Vault()
        {
            var items = await _context.Resources
                .AsNoTracking()
                .Where(r => r.IsPublished)
                .OrderBy(r => r.Category).ThenBy(r => r.SortOrder).ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(items);
        }

        // GET: /Resources/Download/5 — يقدّم ملف المورد للمسجّلين فقط (الملفات خارج wwwroot)
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var resource = await _context.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (resource == null || string.IsNullOrEmpty(resource.FilePath)) return NotFound();
            if (!resource.IsPublished && !User.IsInRole("Admin")) return NotFound();

            var full = FullPath(resource.FilePath);
            if (full == null || !System.IO.File.Exists(full)) return NotFound();

            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var title = lang switch { "en" => resource.TitleEn, "fr" => resource.TitleFr, _ => resource.TitleAr };
            var downloadName = Helpers.SlugHelper.Make(title) is { Length: > 0 } slug ? slug + ".pdf" : "foras-khadra-resource.pdf";
            return PhysicalFile(full, "application/pdf", downloadName, enableRangeProcessing: true);
        }

        // ================= INDEX (admin) =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.Resources
                .AsNoTracking()
                .OrderBy(r => r.Category).ThenBy(r => r.SortOrder).ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(items);
        }

        // ================= CREATE =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() => View(new Resource());

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Resource resource, IFormFile? File)
        {
            if (!await ApplyAsync(resource, resource, File)) return View(resource);

            resource.CreatedAt = DateTime.Now;
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            return resource == null ? NotFound() : View(resource);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Resource resource, IFormFile? File)
        {
            var existing = await _context.Resources.FindAsync(id);
            if (existing == null) return NotFound();

            if (!await ApplyAsync(existing, resource, File))
            {
                resource.FilePath = existing.FilePath;
                return View(resource);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                DeleteFile(resource.FilePath);
                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>ينسخ حقول النموذج إلى الكيان ويتحقق: لا بد من ملف أو رابط؛ الملف PDF فقط بحجم ≤ 10MB وبتوقيع %PDF.</summary>
        private async Task<bool> ApplyAsync(Resource target, Resource form, IFormFile? file)
        {
            ModelState.Remove(nameof(Resource.FilePath));
            ModelState.Remove(nameof(Resource.ExternalUrl));

            var url = (form.ExternalUrl ?? string.Empty).Trim();
            if (url.Length > 0 && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")))
                ModelState.AddModelError(nameof(Resource.ExternalUrl), "الرابط غير صالح — يجب أن يبدأ بـ https://");

            string? newPath = null;
            if (file != null && file.Length > 0)
            {
                var error = await ValidatePdfAsync(file);
                if (error != null) ModelState.AddModelError("File", error);
                else newPath = await SavePdfAsync(file);
            }

            if (newPath == null && string.IsNullOrEmpty(target.FilePath) && url.Length == 0)
                ModelState.AddModelError("File", "أضف ملف PDF أو رابطًا خارجيًا.");

            if (!ModelState.IsValid)
            {
                DeleteFile(newPath);
                return false;
            }

            target.TitleAr = form.TitleAr; target.TitleEn = form.TitleEn; target.TitleFr = form.TitleFr;
            target.DescriptionAr = form.DescriptionAr; target.DescriptionEn = form.DescriptionEn; target.DescriptionFr = form.DescriptionFr;
            target.Category = form.Category;
            target.SortOrder = form.SortOrder;
            target.IsPublished = form.IsPublished;
            target.ExternalUrl = url.Length == 0 ? null : url;

            if (newPath != null)
            {
                DeleteFile(target.FilePath);
                target.FilePath = newPath;
            }
            return true;
        }

        private static async Task<string?> ValidatePdfAsync(IFormFile file)
        {
            if (file.Length > MaxPdfBytes) return "حجم الملف يتجاوز 10MB.";
            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)) return "الملف يجب أن يكون PDF.";

            var head = new byte[5];
            await using var s = file.OpenReadStream();
            var read = await s.ReadAsync(head, 0, head.Length);
            // توقيع PDF: %PDF-
            if (read < 5 || head[0] != 0x25 || head[1] != 0x50 || head[2] != 0x44 || head[3] != 0x46 || head[4] != 0x2D)
                return "الملف ليس PDF صالحًا.";
            return null;
        }

        private string ResourcesFolder => Path.Combine(_env.ContentRootPath, "App_Data", "resources");

        /// <summary>FilePath يخزّن اسم الملف فقط (GUID.pdf)؛ أي قيمة أخرى تُرفض حتى لا يُقرأ ملف خارج المجلد.</summary>
        private string? FullPath(string? storedName)
        {
            if (string.IsNullOrEmpty(storedName) || storedName != Path.GetFileName(storedName) || !storedName.EndsWith(".pdf")) return null;
            return Path.Combine(ResourcesFolder, storedName);
        }

        private async Task<string> SavePdfAsync(IFormFile file)
        {
            Directory.CreateDirectory(ResourcesFolder);
            var name = Guid.NewGuid().ToString("N") + ".pdf";
            await using var fs = new FileStream(Path.Combine(ResourcesFolder, name), FileMode.CreateNew);
            await file.CopyToAsync(fs);
            return name;
        }

        private void DeleteFile(string? storedName)
        {
            var full = FullPath(storedName);
            if (full != null && System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
    }
}
