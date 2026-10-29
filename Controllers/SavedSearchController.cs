using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>حفظ البحث من صفحة كل الفرص وإدارته من "صفحتي". التنبيه بالجديد يتولاه NotificationSweeper.</summary>
    [Authorize]
    public class SavedSearchController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>حد أقصى يمنع التخمة — يكفي أي باحث جاد ويحمي الماسح الدوري.</summary>
        public const int MaxPerUser = 10;

        public SavedSearchController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // POST: /SavedSearch/Save — من شريط "احفظ هذا البحث" في صفحة كل الفرص
        [HttpPost]
        public async Task<IActionResult> Save(string? search, string[]? type, string? country)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var query = (search ?? string.Empty).Trim();
            var typesCsv = string.Join(",", NotificationSweeper.ParseTypes(string.Join(",", type ?? Array.Empty<string>())));
            var countryName = (country ?? string.Empty).Trim();

            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            // لا معنى لحفظ بحث بلا أي معيار
            if (query.Length == 0 && typesCsv.Length == 0 && countryName.Length == 0)
                return RedirectBack(query, type, countryName);

            if (query.Length > 200) query = query[..200];

            var exists = await _db.SavedSearches.AnyAsync(s =>
                s.UserId == userId && s.Query == query && s.TypesCsv == typesCsv && s.Country == countryName);

            if (exists)
            {
                TempData["SavedSearchMsg"] = lang == "en" ? "This search is already saved."
                    : lang == "fr" ? "Cette recherche est déjà enregistrée."
                    : "هذا البحث محفوظ لديك أصلًا.";
            }
            else if (await _db.SavedSearches.CountAsync(s => s.UserId == userId) >= MaxPerUser)
            {
                TempData["SavedSearchMsg"] = lang == "en" ? $"You can keep up to {MaxPerUser} saved searches — delete one from My page first."
                    : lang == "fr" ? $"Vous pouvez garder jusqu'à {MaxPerUser} recherches — supprimez-en une depuis Ma page."
                    : $"يمكنك حفظ {MaxPerUser} بحوث كحد أقصى — احذف واحدًا من صفحتي أولًا.";
            }
            else
            {
                _db.SavedSearches.Add(new SavedSearch
                {
                    UserId = userId,
                    Query = query,
                    TypesCsv = typesCsv,
                    Country = countryName
                });
                await _db.SaveChangesAsync();
                TempData["SavedSearchMsg"] = lang == "en" ? "Search saved — we'll notify you of new matches."
                    : lang == "fr" ? "Recherche enregistrée — nous vous alerterons des nouveautés."
                    : "حُفظ البحث — سنعلمك بكل فرصة جديدة تطابقه.";
            }

            return RedirectBack(query, type, countryName);
        }

        // POST: /SavedSearch/Delete/5 — من قائمة البحوث في "صفحتي"
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _db.SavedSearches
                .Where(s => s.Id == id && s.UserId == userId)
                .ExecuteDeleteAsync();

            return RedirectToAction("Dashboard", "User");
        }

        private IActionResult RedirectBack(string search, string[]? type, string country) =>
            RedirectToAction("AllOpportunities", "Home", new { search, type, country });
    }
}
