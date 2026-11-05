using System.Globalization;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    public sealed record TaskRow(AmbassadorTaskDef Def, AmbassadorTaskSubmission? Submission, int Progress);
    public sealed record AmbassadorVM(string FirstName, string Month, List<TaskRow> Tasks, int Points, int MonthPoints, (string Key, string Title)? Level, (int Needed, string Title)? Next,
        string ReferralCode, int ReferralsTotal, int ReferralsThisMonth, string? University, bool CertificateReady);
    public sealed record UniversityRow(string University, int Members, int Ambassadors, int Points, int Referrals);

    /// <summary>
    /// السفراء الجامعيون (المرحلة 4.4): لوحة /ambassador بمهام الشهر والنقاط والشارة والشهادة (طباعة)، ولوحة الجامعات العامة
    /// /ambassadors/universities. الجامعة تُقرأ من ملف الأهلية (البوصلة).
    /// </summary>
    public class AmbassadorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IReferralService _referrals;

        public AmbassadorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IReferralService referrals)
        {
            _db = db;
            _userManager = userManager;
            _referrals = referrals;
        }

        private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private static string T(string ar, string en, string fr) => Lang == "en" ? en : Lang == "fr" ? fr : ar;

        [Authorize(Roles = "User")]
        [HttpGet("/{culture:culture}/ambassador")]
        [HttpGet("/ambassador", Order = 1)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var vm = await BuildAsync(user);
            ViewData["Title"] = T("لوحة السفير", "Ambassador dashboard", "Tableau de bord ambassadeur");
            ViewBag.Message = TempData["AmbassadorMessage"] as string;
            return View(vm);
        }

        [Authorize(Roles = "User")]
        [HttpPost("/{culture:culture}/ambassador/submit")]
        [HttpPost("/ambassador/submit", Order = 1)]
        [EnableRateLimiting("contact")]
        public async Task<IActionResult> Submit(string taskKey, string? note, string? proofUrl)
        {
            var def = AmbassadorTasks.Find(taskKey);
            var userId = _userManager.GetUserId(User)!;
            if (def == null || def.Key == AmbassadorTasks.InviteKey) return BadRequest();
            var month = AmbassadorTaskSubmission.MonthKey(DateTime.Now);
            proofUrl = HttpUrl.Normalize(proofUrl);
            note = note?.Trim();
            if (def.NeedsProof && proofUrl == null) { TempData["AmbassadorMessage"] = "proof"; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrEmpty(note) && proofUrl == null) { TempData["AmbassadorMessage"] = "empty"; return RedirectToAction(nameof(Index)); }

            var existing = await _db.AmbassadorTaskSubmissions.FirstOrDefaultAsync(s => s.UserId == userId && s.TaskKey == def.Key && s.Month == month);
            if (existing != null && existing.Status != TaskSubmissionStatus.Rejected) { TempData["AmbassadorMessage"] = "already"; return RedirectToAction(nameof(Index)); }
            if (existing == null)
            {
                existing = new AmbassadorTaskSubmission { UserId = userId, TaskKey = def.Key, Month = month };
                _db.AmbassadorTaskSubmissions.Add(existing);
            }
            existing.Note = string.IsNullOrEmpty(note) ? null : note[..Math.Min(500, note.Length)];
            existing.ProofUrl = proofUrl;
            existing.Status = TaskSubmissionStatus.Submitted;
            existing.CreatedAt = DateTime.Now;
            existing.ReviewedAt = null;
            existing.PointsAwarded = 0;
            await _db.SaveChangesAsync();
            TempData["AmbassadorMessage"] = "sent";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>شهادة السفير للطباعة (PDF من المتصفح) — من المستوى البرونزي فصاعدًا.</summary>
        [Authorize(Roles = "User")]
        [HttpGet("/{culture:culture}/ambassador/certificate")]
        [HttpGet("/ambassador/certificate", Order = 1)]
        public async Task<IActionResult> Certificate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var vm = await BuildAsync(user);
            if (!vm.CertificateReady) return RedirectToAction(nameof(Index));
            ViewData["Title"] = T("شهادة سفير", "Ambassador certificate", "Certificat d'ambassadeur");
            ViewData["Robots"] = "noindex";
            return View(vm);
        }

        /// <summary>لوحة الجامعات العامة: أعضاء وسفراء ونقاط وإحالات لكل جامعة (من ملفات الأهلية).</summary>
        [AllowAnonymous]
        [HttpGet("/{culture:culture}/ambassadors/universities")]
        [HttpGet("/ambassadors/universities", Order = 1)]
        public async Task<IActionResult> Universities()
        {
            var profiles = await _db.EligibilityProfiles.AsNoTracking()
                .Where(p => p.University != null && p.University != "")
                .Select(p => new { p.UserId, University = p.University!.Trim() })
                .ToListAsync();
            var byUser = profiles.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.First().University);
            var userIds = byUser.Keys.ToList();

            var points = await _db.AmbassadorTaskSubmissions.AsNoTracking()
                .Where(s => s.Status == TaskSubmissionStatus.Approved && userIds.Contains(s.UserId))
                .GroupBy(s => s.UserId).Select(g => new { g.Key, Points = g.Sum(s => s.PointsAwarded) })
                .ToDictionaryAsync(x => x.Key, x => x.Points);
            var referrals = await _db.Users.AsNoTracking()
                .Where(u => u.ReferredByUserId != null && userIds.Contains(u.ReferredByUserId))
                .GroupBy(u => u.ReferredByUserId!).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var rows = byUser.GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                .Select(g => new UniversityRow(
                    g.Key,
                    g.Count(),
                    g.Count(kv => points.GetValueOrDefault(kv.Key) >= AmbassadorTasks.CertificateMinPoints),
                    g.Sum(kv => points.GetValueOrDefault(kv.Key)),
                    g.Sum(kv => referrals.GetValueOrDefault(kv.Key))))
                .OrderByDescending(r => r.Points).ThenByDescending(r => r.Referrals).ThenByDescending(r => r.Members)
                .Take(30).ToList();

            ViewData["Title"] = T("لوحة الجامعات", "Universities board", "Tableau des universités");
            ViewData["Description"] = T("أي جامعة في الوطن العربي ينشط سفراؤها أكثر في نشر الفرص؟", "Which university in the Arab world has the most active ambassadors?", "Quelle université du monde arabe a les ambassadeurs les plus actifs ?");
            return View(rows);
        }

        private async Task<AmbassadorVM> BuildAsync(ApplicationUser user)
        {
            var now = DateTime.Now;
            var month = AmbassadorTaskSubmission.MonthKey(now);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var code = await _referrals.EnsureCodeAsync(user);
            var referralsTotal = await _referrals.CountAsync(user.Id);
            var referralsMonth = await _db.Users.CountAsync(u => u.ReferredByUserId == user.Id && u.ReferredAt >= monthStart);

            var subs = await _db.AmbassadorTaskSubmissions.Where(s => s.UserId == user.Id).ToListAsync();

            // مهمة الدعوات تُعتمد آليًا حين يبلغ العضو الهدف هذا الشهر
            var invite = AmbassadorTasks.Find(AmbassadorTasks.InviteKey)!;
            if (referralsMonth >= AmbassadorTasks.InviteTarget && !subs.Any(s => s.TaskKey == invite.Key && s.Month == month))
            {
                var auto = new AmbassadorTaskSubmission { UserId = user.Id, TaskKey = invite.Key, Month = month, Status = TaskSubmissionStatus.Approved, PointsAwarded = invite.Points, ReviewedAt = now, Note = "auto" };
                _db.AmbassadorTaskSubmissions.Add(auto);
                await _db.SaveChangesAsync();
                subs.Add(auto);
            }

            var rows = AmbassadorTasks.All.Select(def => new TaskRow(def,
                subs.Where(s => s.TaskKey == def.Key && s.Month == month).OrderByDescending(s => s.CreatedAt).FirstOrDefault(),
                def.Key == AmbassadorTasks.InviteKey ? Math.Min(referralsMonth, AmbassadorTasks.InviteTarget) : 0)).ToList();

            var points = subs.Where(s => s.Status == TaskSubmissionStatus.Approved).Sum(s => s.PointsAwarded);
            var monthPoints = subs.Where(s => s.Status == TaskSubmissionStatus.Approved && s.Month == month).Sum(s => s.PointsAwarded);
            var university = await _db.EligibilityProfiles.AsNoTracking().Where(p => p.UserId == user.Id).Select(p => p.University).FirstOrDefaultAsync();
            var first = (user.FirstName ?? user.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

            return new AmbassadorVM(first, month, rows, points, monthPoints, AmbassadorTasks.LevelFor(points, Lang), AmbassadorTasks.NextLevel(points, Lang),
                code, referralsTotal, referralsMonth, university, points >= AmbassadorTasks.CertificateMinPoints);
        }
    }
}
