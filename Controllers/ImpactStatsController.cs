using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>أرقام الأثر في بانر صفحة "نشاطاتنا": صفحة واحدة تعدّل كل الأرقام دفعة واحدة (للأدمن).</summary>
    [Authorize(Roles = "Admin")]
    public class ImpactStatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ImpactStatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stats = await _context.ImpactStats.AsNoTracking().OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
            return View(stats);
        }

        /// <summary>يحفظ القائمة كما وصلت: تحديث الموجود بالمعرّف، إضافة الجديد (Id = 0)، وحذف ما لم يصل.</summary>
        [HttpPost]
        public async Task<IActionResult> Save(List<ImpactStat> stats)
        {
            stats = (stats ?? new List<ImpactStat>()).Where(s => !s.IsEmpty).ToList();

            var existing = await _context.ImpactStats.ToListAsync();
            var keptIds = stats.Where(s => s.Id > 0).Select(s => s.Id).ToHashSet();
            _context.ImpactStats.RemoveRange(existing.Where(e => !keptIds.Contains(e.Id)));

            var order = 1;
            foreach (var incoming in stats)
            {
                var target = incoming.Id > 0 ? existing.FirstOrDefault(e => e.Id == incoming.Id) : null;
                if (target == null)
                {
                    target = new ImpactStat();
                    _context.ImpactStats.Add(target);
                }
                target.Value = incoming.Value.Trim();
                target.LabelAr = incoming.LabelAr.Trim();
                target.LabelEn = incoming.LabelEn.Trim();
                target.LabelFr = incoming.LabelFr.Trim();
                target.IsVisible = incoming.IsVisible;
                target.SortOrder = order++;
            }

            await _context.SaveChangesAsync();
            TempData["Saved"] = true;
            return RedirectToAction(nameof(Index));
        }
    }
}
