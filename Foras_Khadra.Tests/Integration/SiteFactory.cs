using System.Net.Http;
using System.Text.RegularExpressions;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Foras_Khadra.Tests.Integration
{
    /// <summary>
    /// الموقع كاملًا داخل الاختبار (المرحلة 5.2): Program.cs الحقيقي بكل الوسطاء والمسارات، على SQLite في الذاكرة بدل SQL Server،
    /// بيئة "Testing" (لا بذر تطوير، ترويسات الإنتاج)، بلا بريد حقيقي ولا خدمات خلفية ثقيلة.
    /// </summary>
    public class SiteFactory : WebApplicationFactory<Program>
    {
        public const string TeamKey = "integration-test-team-key-0123456789";
        public const string Password = "Test#Pass123";

        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();
            builder.UseEnvironment("Testing");
            builder.UseSetting("AllowedHosts", "localhost");
            builder.UseSetting("TeamApi:Key", TeamKey);
            builder.UseSetting("Backup:Enabled", "false");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;");

            builder.ConfigureTestServices(services =>
            {
                // SQL Server → SQLite على الاتصال المشترك نفسه (EF 9+ يسجّل إعدادات السياق في IDbContextOptionsConfiguration)
                foreach (var d in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>)).ToList())
                    services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));

                // الخدمات الخلفية الثقيلة لا تعمل في الاختبار (طابور البريد يبقى، لكن المرسِل وهمي)
                foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType != null
                             && (d.ImplementationType == typeof(AnalyticsFlushService)
                                 || d.ImplementationType == typeof(NotificationSweepHostedService)
                                 || d.ImplementationType == typeof(BackupHostedService))).ToList())
                    services.Remove(d);
                services.RemoveAll<IEmailSender>();
                services.AddTransient<IEmailSender, NullEmailSender>();

                // المخطط قبل أن يبدأ Program.cs بإنشاء الأدوار
                using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }

        /// <summary>عميل بلا تتبع تلقائي للتحويلات (نفحص Location بأنفسنا) وعلى https (كالإنتاج).</summary>
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        public async Task<ApplicationUser> CreateUserAsync(string email, string role, bool confirmed = true)
        {
            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = confirmed,
                FullName = "Test " + role, FirstName = "Test", LastName = role, Language = "ar", CreatedAt = DateTime.Now,
                Role = role switch { "Admin" => UserRole.Admin, "Organization" => UserRole.Organization, _ => UserRole.User }
            };
            var created = await users.CreateAsync(user, Password);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
            var roled = await users.AddToRoleAsync(user, role);
            if (!roled.Succeeded) throw new InvalidOperationException(string.Join("; ", roled.Errors.Select(e => e.Description)));
            if (role == "Organization")
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var org = new Organization
                {
                    Name = "Org " + email, Sector = "s", Country = "JO", ContactName = "c", ContactEmail = email,
                    PhoneNumber = "0", Password = "x", UserId = user.Id, CreatedAt = DateTime.Now
                };
                db.Organizations.Add(org);
                db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = "";
                await db.SaveChangesAsync();
            }
            return user;
        }

        public async Task<int> SeedOpportunityAsync(string creatorId, OpportunityType type, OpportunityStatus status = OpportunityStatus.Published,
            string applyLink = "https://apply.example.test/form")
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var opp = new Opportunity
            {
                TitleAr = "فرصة اختبار " + type, TitleEn = "Test opportunity " + type, TitleFr = "Opportunité test " + type,
                DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x",
                DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
                EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x",
                BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
                ApplyLink = applyLink, ImagePath = "/images/icon-192.png", PublishedBy = "x", CreatedByUserId = creatorId,
                Type = type, Status = status, PublishDate = DateTime.Now.AddDays(-1),
                AvailableCountries = new List<Country>()
            };
            db.Opportunities.Add(opp);
            await db.SaveChangesAsync();
            return opp.Id;
        }

        public T InDb<T>(Func<ApplicationDbContext, T> query)
        {
            using var scope = Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }

        public async Task<string> EmailConfirmationCodeAsync(string userId)
        {
            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("user missing");
            return Foras_Khadra.Helpers.TokenCodec.Encode(await users.GenerateEmailConfirmationTokenAsync(user));
        }

        /// <summary>رمز CSRF من صفحة نموذج (الحقل المخفي __RequestVerificationToken) — الكوكي المرافق يبقى في العميل.</summary>
        public static async Task<string> AntiforgeryAsync(HttpClient client, string formUrl)
        {
            var html = await (await client.GetAsync(formUrl)).Content.ReadAsStringAsync();
            var m = Regex.Match(html, "__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            if (!m.Success) throw new InvalidOperationException("antiforgery token not found on " + formUrl);
            return m.Groups[1].Value;
        }

        /// <summary>دخول حقيقي عبر النموذج (CSRF + كوكي Identity)، كما يفعل المتصفح.</summary>
        public static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password, string? returnUrl = null)
        {
            var url = "/Account/Login" + (returnUrl == null ? "" : "?returnUrl=" + Uri.EscapeDataString(returnUrl));
            var token = await AntiforgeryAsync(client, url);
            return await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false", ["__RequestVerificationToken"] = token
            }));
        }

        /// <summary>دخول منظمة عبر نموذجها (/Organization/Login) — صفحة الأفراد ترفض حسابات المنظمات وتعيدها إلى صفحتها.</summary>
        public static async Task<HttpResponseMessage> LoginOrganizationAsync(HttpClient client, string email, string password, string? returnUrl = null)
        {
            var url = "/Organization/Login" + (returnUrl == null ? "" : "?returnUrl=" + Uri.EscapeDataString(returnUrl));
            var token = await AntiforgeryAsync(client, url);
            return await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false", ["__RequestVerificationToken"] = token
            }));
        }

        private sealed class NullEmailSender : IEmailSender
        {
            public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
        }
    }
}
