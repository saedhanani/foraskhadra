
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// الإنشاء والتعديل والحذف للأدمن فقط؛ العرض (Index/Details) عام
[Authorize(Roles = "Admin")]
public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IImageUploadService _images;

    public ArticlesController(ApplicationDbContext context, IImageUploadService images)
    {
        _context = context;
        _images = images;
    }

    // INDEX
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string search, int page = 1)
    {
        const int pageSize = 10;

        var query = _context.Articles.AsQueryable();

     if (!string.IsNullOrWhiteSpace(search))
{
    query = query.Where(a =>
        a.TitleAr.Contains(search) ||
        a.TitleEn.Contains(search) ||
        a.TitleFr.Contains(search));
}


        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var articles = await query
            .OrderByDescending(a => a.PublishDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;

        return View(articles);
    }

    // DETAILS
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
            return NotFound();

        // أحدث المقالات (لـ اقرأ أيضًا)
        ViewBag.LatestArticles = await _context.Articles
            .Where(a => a.Id != id)
            .OrderByDescending(a => a.PublishDate)
            .Take(6) // نأخذ عدد أكبر والفيو يختار 3
            .ToListAsync();

        return View(article);
    }

    // CREATE
    public IActionResult Create()
    {
        ViewBag.IsAdmin = User.IsInRole("Admin");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Article article, IFormFile Image)
    {
        if (User.IsInRole("Admin"))
        {
            article.Author = "فرص خضراء";
        }

        if (Image != null)
        {
            var error = _images.Validate(Image);
            if (error != null)
            {
                ModelState.AddModelError("Image", error);
                ViewBag.IsAdmin = User.IsInRole("Admin");
                return View(article);
            }
            article.ImagePath = await _images.SaveAsync(Image, "uploads");
        }

        // تعقيم HTML القادم من المحرر قبل التخزين
        article.ContentAr = SafeHtml.Clean(article.ContentAr);
        article.ContentEn = SafeHtml.Clean(article.ContentEn);
        article.ContentFr = SafeHtml.Clean(article.ContentFr);

        article.PublishDate = DateTime.Now;

        _context.Add(article);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // EDIT
    public async Task<IActionResult> Edit(int id)
    {
        ViewBag.IsAdmin = User.IsInRole("Admin");
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();
        return View(article);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Article article, IFormFile Image)
    {
        var existing = await _context.Articles.FindAsync(id);
        if (existing == null) return NotFound();

        if (Image != null)
        {
            var error = _images.Validate(Image);
            if (error != null)
            {
                ModelState.AddModelError("Image", error);
                ViewBag.IsAdmin = User.IsInRole("Admin");
                return View(article);
            }
            existing.ImagePath = await _images.SaveAsync(Image, "uploads");
        }

        existing.TitleAr = article.TitleAr;
        existing.TitleEn = article.TitleEn;
        existing.TitleFr = article.TitleFr;
        existing.ContentAr = SafeHtml.Clean(article.ContentAr);
        existing.ContentEn = SafeHtml.Clean(article.ContentEn);
        existing.ContentFr = SafeHtml.Clean(article.ContentFr);
        existing.Author = article.Author;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // DELETE (بدون صفحة)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article != null)
        {
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
