using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>سفراء فرص خضراء (5-3): توليد الرمز، ربط المدعو، العدّ، لوحة الشهر.</summary>
    public class ReferralServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly ReferralService _service;

        public ReferralServiceTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();
            _service = new ReferralService(_db);

            _db.Users.Add(new ApplicationUser { Id = "host", UserName = "host", Email = "host@test.local", FirstName = "سارة" });
            _db.Users.Add(new ApplicationUser { Id = "g1", UserName = "g1", Email = "g1@test.local" });
            _db.Users.Add(new ApplicationUser { Id = "g2", UserName = "g2", Email = "g2@test.local" });
            _db.SaveChanges();
        }

        [Fact]
        public async Task EnsureCode_GeneratesOnce_AndKeepsIt()
        {
            var host = _db.Users.Single(u => u.Id == "host");
            var code = await _service.EnsureCodeAsync(host);
            Assert.Equal(ReferralService.CodeLength, code.Length);
            Assert.Equal(code, await _service.EnsureCodeAsync(host));
            Assert.Equal(code, _db.Users.Single(u => u.Id == "host").ReferralCode);
        }

        [Fact]
        public void GenerateCode_UsesUnambiguousAlphabet()
        {
            for (var i = 0; i < 50; i++)
            {
                var code = ReferralService.GenerateCode();
                Assert.Equal(ReferralService.CodeLength, code.Length);
                Assert.DoesNotContain('0', code); Assert.DoesNotContain('O', code);
                Assert.DoesNotContain('1', code); Assert.DoesNotContain('I', code); Assert.DoesNotContain('L', code);
            }
        }

        [Fact]
        public async Task Apply_LinksGuest_IgnoresUnknown_PreventsSelfAndDouble()
        {
            var host = _db.Users.Single(u => u.Id == "host");
            var code = await _service.EnsureCodeAsync(host);
            var g1 = _db.Users.Single(u => u.Id == "g1");

            Assert.False(await _service.ApplyAsync(g1, "NOPE1234"));
            Assert.False(await _service.ApplyAsync(host, code)); // إحالة ذاتية
            Assert.True(await _service.ApplyAsync(g1, code.ToLowerInvariant())); // غير حساس لحالة الأحرف
            Assert.False(await _service.ApplyAsync(g1, code)); // لا تُربط مرتين

            Assert.Equal("host", _db.Users.Single(u => u.Id == "g1").ReferredByUserId);
            Assert.Equal(1, await _service.CountAsync("host"));
            Assert.Equal(0, await _service.CountAsync("g1"));
        }

        [Fact]
        public async Task MonthlyBoard_RanksByReferralsThisMonth_FirstNamesOnly()
        {
            var host = _db.Users.Single(u => u.Id == "host");
            var code = await _service.EnsureCodeAsync(host);
            await _service.ApplyAsync(_db.Users.Single(u => u.Id == "g1"), code);
            await _service.ApplyAsync(_db.Users.Single(u => u.Id == "g2"), code);

            // إحالة قديمة من شهر سابق لا تُحسب
            _db.Users.Single(u => u.Id == "g2").ReferredAt = DateTime.Now.AddMonths(-2);
            _db.SaveChanges();

            var board = await _service.MonthlyBoardAsync(10);
            var row = Assert.Single(board);
            Assert.Equal("سارة", row.FirstName);
            Assert.Equal(1, row.Referrals);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
