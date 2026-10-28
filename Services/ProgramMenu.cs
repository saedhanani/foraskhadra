using Foras_Khadra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Foras_Khadra.Services
{
    /// <summary>بند في قائمة "نشاطاتنا" المنسدلة.</summary>
    public record ProgramMenuItem(string Slug, string Ar, string En, string Fr)
    {
        public string Title(string lang) => lang switch { "en" => En, "fr" => Fr, _ => Ar };
    }

    /// <summary>المشاريع الرائدة المنشورة للقائمة العلوية — مخزّنة مؤقتًا لأن القالب يطلبها في كل صفحة.</summary>
    public interface IProgramMenu
    {
        Task<IReadOnlyList<ProgramMenuItem>> GetAsync();
        void Invalidate();
    }

    public class ProgramMenu : IProgramMenu
    {
        private const string CacheKey = "program-menu";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public ProgramMenu(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IReadOnlyList<ProgramMenuItem>> GetAsync()
        {
            if (_cache.TryGetValue(CacheKey, out IReadOnlyList<ProgramMenuItem>? cached) && cached != null)
                return cached;

            IReadOnlyList<ProgramMenuItem> items;
            try
            {
                items = await _context.FlagshipPrograms
                    .AsNoTracking()
                    .Where(p => p.IsPublished)
                    .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
                    .Select(p => new ProgramMenuItem(p.Slug, p.MenuTitleAr, p.MenuTitleEn, p.MenuTitleFr))
                    .ToListAsync();
            }
            catch (Exception)
            {
                // الجدول لم يُنشأ بعد على الخادم: القائمة تعمل بلا مشاريع بدل أن تسقط كل الصفحات
                items = Array.Empty<ProgramMenuItem>();
            }

            _cache.Set(CacheKey, items, TimeSpan.FromMinutes(10));
            return items;
        }

        public void Invalidate() => _cache.Remove(CacheKey);
    }
}
