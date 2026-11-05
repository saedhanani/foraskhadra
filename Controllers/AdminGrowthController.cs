using System.Security.Claims;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Controllers
{
    /// <summary>
    /// لوحة المرحلة 4 (المسارات تحت /Admin/…): أدلة التقديم، قصص النجاح، أسئلة الفرص، مهام السفراء، الإبراز والتعزيز.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("Admin/[action]")]
    public class AdminGrowthController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBoostService _boosts;

        public AdminGrowthController(ApplicationDbContext db, IBoostService boosts)
        {
            _db = db;
            _boosts = boosts;
        }

        private string AdminId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        // ===== أدلة التقديم (4.1) =====
        [HttpGet]
        public async Task<IActionResult> Guides()
        {
            var today = DateTime.Now.Date;
            var guides = await _db.ApplicationGuides.AsNoTracking().OrderBy(g => g.SortOrder).ThenBy(g => g.TitleAr).ToListAsync();
            var keys = guides.Select(g => g.ProgramKey).ToList();
            ViewBag.OpenCounts = await _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && o.ProgramKey != null && keys.Contains(o.ProgramKey)
                    && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today))
                .GroupBy(o => o.ProgramKey!).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            ViewBag.KnownKeys = await _db.Opportunities.AsNoTracking().Where(o => o.ProgramKey != null && o.ProgramKey != "").Select(o => o.ProgramKey!).Distinct().OrderBy(k => k).ToListAsync();
            return View(guides);
        }

        [HttpGet("{id?}")]
        public async Task<IActionResult> GuideEdit(int? id)
        {
            if (id == null) return View(new GuideForm { IsPublished = true });
            var g = await _db.ApplicationGuides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return g == null ? NotFound() : View(GuideForm.From(g));
        }

        [HttpPost("{id?}")]
        public async Task<IActionResult> GuideEdit(GuideForm form)
        {
            if (!ModelState.IsValid) return View(form);
            var key = form.ProgramKey.Trim().ToLowerInvariant();
            var slug = form.Slug.Trim().ToLowerInvariant();
            if (await _db.ApplicationGuides.AnyAsync(g => g.Id != form.Id && (g.Slug == slug || g.ProgramKey == key)))
            {
                ModelState.AddModelError("", "الرابط أو مفتاح البرنامج مستعمل في دليل آخر.");
                return View(form);
            }
            ApplicationGuide guide;
            if (form.Id == 0) { guide = new ApplicationGuide(); _db.ApplicationGuides.Add(guide); }
            else { guide = await _db.ApplicationGuides.FirstAsync(g => g.Id == form.Id); }
            form.ApplyTo(guide);
            await _db.SaveChangesAsync();
            TempData["GuideMessage"] = "حُفظ الدليل.";
            return RedirectToAction(nameof(Guides));
        }

        [HttpPost]
        public async Task<IActionResult> GuideDelete(int id)
        {
            var g = await _db.ApplicationGuides.FirstOrDefaultAsync(x => x.Id == id);
            if (g != null) { _db.ApplicationGuides.Remove(g); await _db.SaveChangesAsync(); }
            TempData["GuideMessage"] = "حُذف الدليل.";
            return RedirectToAction(nameof(Guides));
        }

        [HttpPost]
        public async Task<IActionResult> GuidesSeed()
        {
            var existing = (await _db.ApplicationGuides.Select(g => g.ProgramKey).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = 0;
            foreach (var g in ApplicationGuideSeed.All())
            {
                if (existing.Contains(g.ProgramKey)) continue;
                _db.ApplicationGuides.Add(g);
                added++;
            }
            await _db.SaveChangesAsync();
            TempData["GuideMessage"] = added == 0 ? "لا جديد: كل أدلة الدفعة الأولى موجودة." : $"أُضيف {added} دليلًا.";
            return RedirectToAction(nameof(Guides));
        }

        // ===== قصص النجاح (4.2) =====
        [HttpGet]
        public async Task<IActionResult> Stories(string? status = null)
        {
            var all = status == "all";
            ViewBag.ShowingAll = all;
            var q = _db.SuccessStories.AsNoTracking().Include(s => s.User).Include(s => s.Opportunity).AsQueryable();
            if (!all) q = q.Where(s => s.Status == StoryStatus.Pending);
            return View(await q.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedAt).Take(200).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> StoryReview(int id, string action)
        {
            var s = await _db.SuccessStories.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            switch (action)
            {
                case "approve":
                    s.Status = StoryStatus.Approved; s.ReviewedAt = DateTime.Now; s.ReviewedByUserId = AdminId;
                    s.ShownName = StoriesController.ShownNameFor(s.User, s.DisplayName);
                    break;
                case "reject": s.Status = StoryStatus.Rejected; s.ReviewedAt = DateTime.Now; s.ReviewedByUserId = AdminId; s.Featured = false; break;
                case "feature": s.Featured = true; break;
                case "unfeature": s.Featured = false; break;
                case "delete": _db.SuccessStories.Remove(s); break;
            }
            await _db.SaveChangesAsync();
            TempData["StoryMessage"] = "تم.";
            return RedirectToAction(nameof(Stories));
        }

        // ===== أسئلة الفرص (4.6) =====
        [HttpGet]
        public async Task<IActionResult> Questions(string? status = null)
        {
            var all = status == "all";
            ViewBag.ShowingAll = all;
            var q = _db.OpportunityQuestions.AsNoTracking().Include(x => x.Opportunity).Include(x => x.User).AsQueryable();
            if (!all) q = q.Where(x => x.Status == QuestionStatus.Pending);
            return View(await q.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).Take(300).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> QuestionAnswer(int id, string? questionAr, string? questionEn, string? questionFr, string? answerAr, string? answerEn, string? answerFr, string action)
        {
            var x = await _db.OpportunityQuestions.FirstOrDefaultAsync(q => q.Id == id);
            if (x == null) return NotFound();
            if (action == "delete") { _db.OpportunityQuestions.Remove(x); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Questions)); }
            x.QuestionAr = Trim(questionAr, OpportunityQuestion.MaxQuestion) ?? x.QuestionAr;
            x.QuestionEn = Trim(questionEn, OpportunityQuestion.MaxQuestion);
            x.QuestionFr = Trim(questionFr, OpportunityQuestion.MaxQuestion);
            x.AnswerAr = Trim(answerAr, OpportunityQuestion.MaxAnswer);
            x.AnswerEn = Trim(answerEn, OpportunityQuestion.MaxAnswer);
            x.AnswerFr = Trim(answerFr, OpportunityQuestion.MaxAnswer);
            if (x.HasAnswer) { x.AnsweredAt = DateTime.Now; x.AnsweredByUserId = AdminId; }
            x.Status = action == "hide" ? QuestionStatus.Hidden : action == "publish" ? QuestionStatus.Published : x.Status;
            await _db.SaveChangesAsync();
            TempData["QuestionMessage"] = "حُفظ.";
            return RedirectToAction(nameof(Questions), new { status = "all" });
        }

        [HttpPost]
        public async Task<IActionResult> QuestionAdd(int opportunityId, string questionAr, string? questionEn, string? questionFr, string? answerAr, string? answerEn, string? answerFr)
        {
            if (string.IsNullOrWhiteSpace(questionAr) || !await _db.Opportunities.AnyAsync(o => o.Id == opportunityId)) return BadRequest();
            var x = new OpportunityQuestion
            {
                OpportunityId = opportunityId, IsCurated = true, Status = QuestionStatus.Published, Lang = "ar",
                QuestionAr = Trim(questionAr, OpportunityQuestion.MaxQuestion)!, QuestionEn = Trim(questionEn, OpportunityQuestion.MaxQuestion), QuestionFr = Trim(questionFr, OpportunityQuestion.MaxQuestion),
                AnswerAr = Trim(answerAr, OpportunityQuestion.MaxAnswer), AnswerEn = Trim(answerEn, OpportunityQuestion.MaxAnswer), AnswerFr = Trim(answerFr, OpportunityQuestion.MaxAnswer),
                AnsweredAt = DateTime.Now, AnsweredByUserId = AdminId
            };
            _db.OpportunityQuestions.Add(x);
            await _db.SaveChangesAsync();
            TempData["QuestionMessage"] = "أُضيف السؤال المنسَّق.";
            return RedirectToAction(nameof(Questions), new { status = "all" });
        }

        // ===== مهام السفراء (4.4) =====
        [HttpGet]
        public async Task<IActionResult> AmbassadorTasks(string? status = null)
        {
            var all = status == "all";
            ViewBag.ShowingAll = all;
            var q = _db.AmbassadorTaskSubmissions.AsNoTracking().Include(s => s.User).AsQueryable();
            if (!all) q = q.Where(s => s.Status == TaskSubmissionStatus.Submitted);
            return View(await q.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedAt).Take(300).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> TaskReview(int id, string action)
        {
            var s = await _db.AmbassadorTaskSubmissions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            var def = Helpers.AmbassadorTasks.Find(s.TaskKey);
            s.Status = action == "approve" ? TaskSubmissionStatus.Approved : TaskSubmissionStatus.Rejected;
            s.PointsAwarded = s.Status == TaskSubmissionStatus.Approved ? def?.Points ?? 0 : 0;
            s.ReviewedAt = DateTime.Now;
            s.ReviewedByUserId = AdminId;
            await _db.SaveChangesAsync();
            TempData["TaskMessage"] = s.Status == TaskSubmissionStatus.Approved ? $"اعتُمدت ({s.PointsAwarded} نقطة)." : "رُفضت.";
            return RedirectToAction(nameof(AmbassadorTasks));
        }

        // ===== الإبراز والتعزيز (4.5) =====
        [HttpGet]
        public async Task<IActionResult> Promote(int? id, string? q)
        {
            var today = DateTime.Now.Date;
            var list = _db.Opportunities.AsNoTracking()
                .Where(o => o.Status == OpportunityStatus.Published && (o.DeadlineType != DeadlineType.SpecificDate || o.EndDate == null || o.EndDate >= today));
            if (!string.IsNullOrWhiteSpace(q)) list = list.Where(o => o.TitleAr.Contains(q) || o.TitleEn.Contains(q) || o.PublishedBy.Contains(q));
            ViewBag.Query = q;
            ViewBag.Sponsored = await _db.Opportunities.AsNoTracking().Where(o => o.IsSponsored).OrderByDescending(o => o.SponsoredUntil).ToListAsync();
            ViewBag.Boosts = await _db.OpportunityBoosts.AsNoTracking().Include(b => b.Opportunity).OrderByDescending(b => b.CreatedAt).Take(30).ToListAsync();
            ViewBag.Selected = id.HasValue ? await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id) : null;
            ViewBag.Estimate = id.HasValue ? await _boosts.EstimateAsync(id.Value) : (int?)null;
            return View(await list.OrderByDescending(o => o.IsSponsored).ThenByDescending(o => o.PublishDate).Take(40).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SetSponsored(int id, bool sponsored, DateTime? until)
        {
            var o = await _db.Opportunities.FirstOrDefaultAsync(x => x.Id == id);
            if (o == null) return NotFound();
            o.IsSponsored = sponsored;
            o.SponsoredUntil = sponsored ? until : null;
            await _db.SaveChangesAsync();
            TempData["PromoteMessage"] = sponsored ? "صارت الفرصة مموَّلة (تظهر أولًا بعلامة «إعلان مموَّل»)." : "أُلغي الإبراز.";
            return RedirectToAction(nameof(Promote), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Boost(int id, string? note)
        {
            var boost = await _boosts.SendAsync(id, AdminId, note);
            TempData["PromoteMessage"] = boost == null ? "تعذّر التعزيز: الفرصة غير منشورة أو منتهية." : $"أُرسل التعزيز إلى {boost.Recipients} عضوًا مؤهَّلًا (الجرس + الدفع).";
            return RedirectToAction(nameof(Promote), new { id });
        }

        private static string? Trim(string? s, int max)
        {
            s = s?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            return s.Length > max ? s[..max] : s;
        }
    }
}
