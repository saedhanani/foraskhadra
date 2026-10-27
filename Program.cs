using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== إعداد قاعدة البيانات =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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
    Console.WriteLine("Startup Identity Error: " + ex.Message);
}
// ✅ Seed Articles (Test Data)
// =========================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        DbInitializer.SeedArticles(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Articles Seed Error: " + ex.Message);
    }
}

// ✅ معاينة شبكة المنظمات وبيانات تجريبية أخرى (Development فقط — لا تُضاف في Production)
if (app.Environment.IsDevelopment())
{
    using var previewScope = app.Services.CreateScope();
    var previewDb = previewScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        DbInitializer.SeedOrganizationsMapPreview(previewDb);
        DbInitializer.SeedOrganizations(previewDb);
        DbInitializer.SeedOpportunities(previewDb);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Seed Error: " + ex.Message);
    }
}
// ===== Configure the HTTP request pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ❗ خليها بعد exception handling مباشرة
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseStaticFiles();
// ===== Use Localization =====
var localizationOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>()
    .Value;

app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// واجهة منصة الفريق: Controllers/Api (توجيه بالسمات)
app.MapControllers();

app.MapRazorPages();

app.Run();
