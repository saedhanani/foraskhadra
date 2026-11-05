using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Foras_Khadra.Models;
using Xunit;

namespace Foras_Khadra.Tests.Integration
{
    /// <summary>
    /// اختبارات تكامل عبر HTTP على الموقع الحقيقي (المرحلة 5.2): الملكية، الأدوار، مفتاح المنصة، الدخول/التأكيد، اللغة من المسار، بوابة /go/.
    /// تعمل محليًا وفي GitHub Actions (.github/workflows/ci.yml) على SQLite — لا تحتاج SQL Server ولا إعدادات سرية.
    /// </summary>
    public class IntegrationTests : IClassFixture<SiteFactory>
    {
        private readonly SiteFactory _site;
        private static int _seq;

        public IntegrationTests(SiteFactory site) => _site = site;

        private static string Email(string prefix) => $"{prefix}-{Interlocked.Increment(ref _seq)}-{Guid.NewGuid():N}@test.local";

        [Fact]
        public async Task HealthEndpointReportsHealthy()
        {
            var res = await _site.Client().GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("Healthy", await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PublicPageCarriesSecurityHeadersAndArabicDefault()
        {
            var res = await _site.Client().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Contains("default-src 'self'", res.Headers.GetValues("Content-Security-Policy").Single());
            Assert.Equal("SAMEORIGIN", res.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
            var html = await res.Content.ReadAsStringAsync();
            Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html);
        }

        [Fact]
        public async Task LanguageComesFromThePathOnly()
        {
            var client = _site.Client();

            var en = await client.GetAsync("/en/");
            Assert.Equal(HttpStatusCode.OK, en.StatusCode);
            Assert.Contains("<html lang=\"en\" dir=\"ltr\"", await en.Content.ReadAsStringAsync());

            var fr = await client.GetAsync("/fr/Home/AllOpportunities");
            Assert.Equal(HttpStatusCode.OK, fr.StatusCode);
            var frHtml = await fr.Content.ReadAsStringAsync();
            Assert.Contains("<html lang=\"fr\" dir=\"ltr\"", frHtml);
            Assert.Contains("href=\"/fr/", frHtml); // الروابط المولَّدة تحمل بادئة اللغة

            var legacy = await client.GetAsync("/Home/AllOpportunities?culture=en&type=Jobs");
            Assert.Equal(HttpStatusCode.MovedPermanently, legacy.StatusCode);
            Assert.StartsWith("/en/Home/AllOpportunities", legacy.Headers.Location!.ToString());
            Assert.Contains("type=Jobs", legacy.Headers.Location!.ToString());
            Assert.DoesNotContain("culture=", legacy.Headers.Location!.ToString());

            var unknown = await client.GetAsync("/de/");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }

        [Fact]
        public async Task AnonymousVisitorsAreSentToTheRightLoginPage()
        {
            var client = _site.Client();

            var admin = await client.GetAsync("/Admin/Dashboard");
            Assert.Equal(HttpStatusCode.Found, admin.StatusCode);
            Assert.Equal("/Account/Login?returnUrl=%2FAdmin%2FDashboard", admin.Headers.Location!.ToString());

            var org = await client.GetAsync("/Opportunity/OrgOpportunities");
            Assert.Equal(HttpStatusCode.Found, org.StatusCode);
            Assert.Equal("/Organization/Login?returnUrl=%2FOpportunity%2FOrgOpportunities", org.Headers.Location!.ToString());

            var localized = await client.GetAsync("/en/Admin/Dashboard");
            Assert.Equal(HttpStatusCode.Found, localized.StatusCode);
            Assert.StartsWith("/en/Account/Login?returnUrl=", localized.Headers.Location!.ToString());

            var api = await client.GetAsync("/api/v1/opportunities/does-not-exist");
            Assert.NotEqual(HttpStatusCode.Found, api.StatusCode); // الواجهات لا تحوَّل إلى صفحة دخول
        }

        [Fact]
        public async Task RolesAreEnforcedForSignedInUsers()
        {
            var member = await _site.CreateUserAsync(Email("member"), "User");
            var memberClient = _site.Client();
            var login = await SiteFactory.LoginAsync(memberClient, member.Email!, SiteFactory.Password);
            Assert.Equal(HttpStatusCode.Found, login.StatusCode);
            Assert.Equal("/User/Dashboard", login.Headers.Location!.ToString());

            Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/Admin/Dashboard")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/Opportunity/OrgOpportunities")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/Opportunity/Create")).StatusCode);

            var admin = await _site.CreateUserAsync(Email("admin"), "Admin");
            var adminClient = _site.Client();
            var adminLogin = await SiteFactory.LoginAsync(adminClient, admin.Email!, SiteFactory.Password);
            Assert.Equal(HttpStatusCode.Found, adminLogin.StatusCode);
            Assert.Equal("/Admin/Dashboard", adminLogin.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/Admin/Dashboard")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/Admin/Analytics?days=30")).StatusCode);
        }

        [Fact]
        public async Task LoginRejectsWrongPasswordAndHonoursReturnUrl()
        {
            var user = await _site.CreateUserAsync(Email("login"), "User");
            var client = _site.Client();

            var wrong = await SiteFactory.LoginAsync(client, user.Email!, "Wrong#Pass123");
            Assert.Equal(HttpStatusCode.OK, wrong.StatusCode); // النموذج يُعاد بالخطأ، بلا كوكي دخول
            Assert.DoesNotContain(wrong.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : Array.Empty<string>(),
                c => c.StartsWith(".AspNetCore.Identity.Application="));

            var right = await SiteFactory.LoginAsync(client, user.Email!, SiteFactory.Password, returnUrl: "/Home/AllOpportunities?type=Jobs");
            Assert.Equal(HttpStatusCode.Found, right.StatusCode);
            Assert.Equal("/Home/AllOpportunities?type=Jobs", right.Headers.Location!.ToString());
            Assert.Contains(right.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Identity.Application="));
        }

        [Fact]
        public async Task UnconfirmedMembersMustConfirmEmailBeforeApplyingToScholarships()
        {
            var user = await _site.CreateUserAsync(Email("unconfirmed"), "User", confirmed: false);
            var admin = await _site.CreateUserAsync(Email("admin-seed"), "Admin");
            var id = await _site.SeedOpportunityAsync(admin.Id, OpportunityType.Scholarships, applyLink: "https://scholarship.example.test/apply");
            var client = _site.Client();
            Assert.Equal(HttpStatusCode.Found, (await SiteFactory.LoginAsync(client, user.Email!, SiteFactory.Password)).StatusCode);

            var blocked = await client.GetAsync($"/go/{id}");
            Assert.Equal(HttpStatusCode.Found, blocked.StatusCode);
            Assert.StartsWith("/Account/ConfirmRequired", blocked.Headers.Location!.ToString());

            var code = await _site.EmailConfirmationCodeAsync(user.Id);
            var confirm = await client.GetAsync($"/Account/ConfirmEmail?userId={user.Id}&code={code}");
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            Assert.True(_site.InDb(db => db.Users.Single(u => u.Id == user.Id).EmailConfirmed));

            var allowed = await client.GetAsync($"/go/{id}");
            Assert.Equal(HttpStatusCode.Found, allowed.StatusCode);
            Assert.Equal("https://scholarship.example.test/apply", allowed.Headers.Location!.ToString());
        }

        [Fact]
        public async Task GoGateCountsClicksAndLetsGuestsThroughForJobsOnly()
        {
            var admin = await _site.CreateUserAsync(Email("admin-go"), "Admin");
            var job = await _site.SeedOpportunityAsync(admin.Id, OpportunityType.Jobs, applyLink: "https://jobs.example.test/apply?x=1");
            var scholarship = await _site.SeedOpportunityAsync(admin.Id, OpportunityType.Scholarships);
            var draft = await _site.SeedOpportunityAsync(admin.Id, OpportunityType.Jobs, OpportunityStatus.Draft);
            var guest = _site.Client();

            var viaJob = await guest.GetAsync($"/go/{job}");
            Assert.Equal(HttpStatusCode.Found, viaJob.StatusCode);
            Assert.Equal("https://jobs.example.test/apply?x=1", viaJob.Headers.Location!.ToString());
            Assert.Equal(1, _site.InDb(db => db.OpportunityDailyStats.Where(s => s.OpportunityId == job).Sum(s => s.ApplyClicks)));

            var viaScholarship = await guest.GetAsync($"/go/{scholarship}");
            Assert.Equal(HttpStatusCode.Found, viaScholarship.StatusCode);
            Assert.Equal($"/Account/Login?returnUrl=%2FOpportunity%2FDetails%2F{scholarship}", viaScholarship.Headers.Location!.ToString());
            Assert.Equal(0, _site.InDb(db => db.OpportunityDailyStats.Where(s => s.OpportunityId == scholarship).Sum(s => s.ApplyClicks)));

            Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/go/{draft}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/go/999999")).StatusCode);

            var details = await guest.GetAsync($"/Opportunity/Details/{job}");
            Assert.Equal(HttpStatusCode.OK, details.StatusCode);
            Assert.Contains("/go/" + job, await details.Content.ReadAsStringAsync()); // رابط التقديم ظاهر للزائر في الوظائف
        }

        [Fact]
        public async Task OrganizationsCanEditOnlyTheirOwnOpportunities()
        {
            var owner = await _site.CreateUserAsync(Email("org-owner"), "Organization");
            var other = await _site.CreateUserAsync(Email("org-other"), "Organization");
            var admin = await _site.CreateUserAsync(Email("admin-own"), "Admin");
            var id = await _site.SeedOpportunityAsync(owner.Id, OpportunityType.Internships);

            // حساب منظمة لا يدخل من صفحة الأفراد: يُعاد إلى صفحة الدخول بلا كوكي
            var otherClient = _site.Client();
            var viaIndividual = await SiteFactory.LoginAsync(otherClient, other.Email!, SiteFactory.Password);
            Assert.Equal(HttpStatusCode.Found, viaIndividual.StatusCode);
            Assert.Equal("/Account/Login", viaIndividual.Headers.Location!.ToString());
            Assert.DoesNotContain(viaIndividual.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : Array.Empty<string>(),
                c => c.StartsWith(".AspNetCore.Identity.Application="));
            Assert.Equal(HttpStatusCode.Found, (await otherClient.GetAsync($"/Opportunity/Edit/{id}")).StatusCode); // ما زال زائرًا

            var otherLogin = await SiteFactory.LoginOrganizationAsync(otherClient, other.Email!, SiteFactory.Password);
            Assert.Equal(HttpStatusCode.Found, otherLogin.StatusCode);
            Assert.Equal("/", otherLogin.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.GetAsync($"/Opportunity/Edit/{id}")).StatusCode);

            var ownerClient = _site.Client();
            Assert.Equal(HttpStatusCode.Found, (await SiteFactory.LoginOrganizationAsync(ownerClient, owner.Email!, SiteFactory.Password)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/Opportunity/Edit/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync("/Opportunity/OrgOpportunities")).StatusCode);

            var adminClient = _site.Client();
            Assert.Equal(HttpStatusCode.Found, (await SiteFactory.LoginAsync(adminClient, admin.Email!, SiteFactory.Password)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync($"/Opportunity/Edit/{id}")).StatusCode);
        }

        [Fact]
        public async Task TeamApiRequiresTheSharedKey()
        {
            var client = _site.Client();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/team/ping")).StatusCode);

            using var wrong = new HttpRequestMessage(HttpMethod.Get, "/api/team/ping");
            wrong.Headers.Add("X-Team-Key", "not-the-key-but-long-enough-000000");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrong)).StatusCode);

            using var right = new HttpRequestMessage(HttpMethod.Get, "/api/team/ping");
            right.Headers.Add("X-Team-Key", SiteFactory.TeamKey);
            var ok = await client.SendAsync(right);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            var json = await ok.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("ok").GetBoolean());
        }

        [Fact]
        public async Task FeedsAndTypePagesServePublishedOpportunities()
        {
            var admin = await _site.CreateUserAsync(Email("admin-feed"), "Admin");
            var id = await _site.SeedOpportunityAsync(admin.Id, OpportunityType.Jobs);
            var client = _site.Client();

            var feed = await client.GetAsync("/feed/jobs.xml");
            Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
            Assert.StartsWith("application/atom+xml", feed.Content.Headers.ContentType!.ToString());
            Assert.Contains($"opportunity/{id}", await feed.Content.ReadAsStringAsync());

            var page = await client.GetAsync("/en/opportunities/jobs");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains("Test opportunity Jobs", await page.Content.ReadAsStringAsync());
        }
    }
}
