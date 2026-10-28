using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== Logging (Serilog): الكونسول + ملف يومي في logs/ (يُحتفظ بـ 30 يومًا) =====
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// ===== إعداد قاعدة البيانات =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===== مفاتيح حماية البيانات (كوكي الدخول ورموز CSRF) تُحفظ في App_Data حتى لا يُطرد الجميع عند كل إعادة تشغيل للتطبيق على الاستضافة =====
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")))
    .SetApplicationName("Foras_Khadra");

// ===== إعداد Identity =====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    // قفل الحساب 15 دقيقة بعد 5 محاولات فاشلة (ضد تخمين كلمات المرور)
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===== إعداد الكوكيز =====
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Organization/Login";
    options.AccessDeniedPath = "/Organization/Login";
});

// ===== إعداد خدمات البريد =====
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
builder.Services.AddScoped<ISavedOpportunityService, SavedOpportunityService>();

// الواجهة العامة للقراءة (api/v1) تُستدعى من مواقع أخرى بالمتصفح: GET فقط من أي أصل
builder.Services.AddCors(options => options.AddPolicy("public-api", policy =>
    policy.AllowAnyOrigin().WithMethods("GET").WithHeaders("Accept", "Accept-Language")));

// ===== Session & Cache =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services
    // التحقق من رمز CSRF تلقائيًا في كل POST/PUT/DELETE (واجهة المنصة مستثناة بـ IgnoreAntiforgeryToken)
    .AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();
// ===== Localization (اللغات) =====
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[]
{
    new CultureInfo("ar"),
    new CultureInfo("en"),
    new CultureInfo("fr")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ar"); // الافتراضي عربي
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ===== Rate limiting: الدخول/التسجيل/استعادة كلمة المرور، نموذج التواصل، وواجهة المنصة (لكل عنوان IP) =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync("محاولات كثيرة جدًا، حاول مرة أخرى بعد قليل.", token);
    };

    static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("contact", ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("api", ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ===== ضغط الاستجابات + فحص الصحة (/health يتحقق من الاتصال بقاعدة البيانات) =====
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// ===== إنشاء الأدوار وحساب الأدمن وربط المنظمات =====
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { "Admin", "User", "Organization" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // حساب الأدمن الأول يُزرع فقط إذا ضُبط في الإعدادات (Seed:AdminEmail / Seed:AdminPassword) — لا كلمات مرور في الكود
        string? adminEmail = builder.Configuration["Seed:AdminEmail"];
        string? adminPassword = builder.Configuration["Seed:AdminPassword"];

        var adminUser = string.IsNullOrWhiteSpace(adminEmail) ? null : await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Admin",
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Startup Identity Error");
}

// ✅ بيانات تجريبية (مقالات، معاينة شبكة المنظمات، منظمات، فرص) — Development فقط، لا تُضاف في Production
if (app.Environment.IsDevelopment())
{
    using var previewScope = app.Services.CreateScope();
    var previewDb = previewScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        DbInitializer.SeedArticles(previewDb);
        DbInitializer.SeedOrganizationsMapPreview(previewDb);
        DbInitializer.SeedOrganizations(previewDb);
        DbInitializer.SeedOpportunities(previewDb);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Seed Error");
    }
}
// ===== ملء نص البحث للفرص القديمة (مرة واحدة بعد إضافة العمود؛ الحفظ اللاحق يحدّثه تلقائيًا) =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var missing = await db.Opportunities.Where(o => o.SearchText == null).ToListAsync();
        if (missing.Count > 0)
        {
            foreach (var o in missing) o.SearchText = Foras_Khadra.Helpers.SearchIndexer.Build(o);
            await db.SaveChangesAsync();
            app.Logger.LogInformation("SearchText backfilled for {Count} opportunities", missing.Count);
        }
    }
    catch (Exception ex)
    {
        // لا يوقف التشغيل: مثلًا العمود لم يُضف بعد على الخادم (شغّل Migrations/Sql/2026-10-search-text.sql)
        app.Logger.LogError(ex, "SearchText backfill failed");
    }
}

// ===== Configure the HTTP request pipeline =====
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

// ❗ خليها بعد exception handling مباشرة
app.UseStatusCodePages();

app.UseHttpsRedirection();

// ===== ترويسات أمنية أساسية =====
// CSP: الصفحات تعتمد على سكربتات وأنماط مضمّنة، لذا 'unsafe-inline' مسموح لهما؛ القيمة الحقيقية هنا هي حصر
// مصادر السكربتات والإطارات والاتصالات في قائمة معروفة (لا يمكن لسكربت محقون تحميل كود من خادم غريب أو تسريب بيانات إليه).
const string ContentSecurityPolicy =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://code.iconify.design https://cdn.quilljs.com https://www.google.com https://www.gstatic.com; " +
    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://cdn.quilljs.com; " +
    "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
    "img-src 'self' data: blob: https:; " +
    "connect-src 'self' https://ipapi.co https://api.iconify.design https://api.simplesvg.com https://api.unisvg.com; " +
    "frame-src https://www.google.com; " +
    "frame-ancestors 'self'; " +
    "object-src 'none'; " +
    "base-uri 'self'";

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] = ContentSecurityPolicy;
    await next();
});

// الملفات الثابتة تُخزَّن في المتصفح يومًا واحدًا (الرفوعات تأخذ أسماء عشوائية جديدة عند التغيير)
app.UseStaticFiles(new StaticFileOptions
{
    // sw.js يُستثنى حتى يلتقط المتصفح أي تحديث له فورًا
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl =
        ctx.File.Name == "sw.js" ? "no-cache" : "public,max-age=86400"
});
// ===== Use Localization =====
var localizationOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>()
    .Value;

app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// روابط وصفية لمحركات البحث: /Opportunity/Details/12/منحة-ماجستير — المقطع الأخير تجميلي ويُتجاهل
app.MapControllerRoute(
    name: "opportunity-details",
    pattern: "Opportunity/Details/{id:int}/{slug?}",
    defaults: new { controller = "Opportunity", action = "Details" });

app.MapControllerRoute(
    name: "article-details",
    pattern: "Articles/Details/{id:int}/{slug?}",
    defaults: new { controller = "Articles", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// واجهة منصة الفريق: Controllers/Api (توجيه بالسمات)
app.MapControllers();

app.MapRazorPages();

// فحص الصحة للمراقبة الخارجية (UptimeRobot وأمثاله)
app.MapHealthChecks("/health");

app.Run();
