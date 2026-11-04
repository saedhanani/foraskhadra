using System.IO.Compression;
using System.Text.Json;
using Foras_Khadra.Data;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Foras_Khadra.Tests
{
    public class BackupExporterTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "fk-backup-tests-" + Guid.NewGuid().ToString("N"));

        public BackupExporterTests()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
            _db = new ApplicationDbContext(options);
            _db.Database.EnsureCreated();

            _db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local", PasswordHash = "SECRET-HASH", SecurityStamp = "SECRET-STAMP" });
            var org = new Organization { Id = 1, Name = "Org", Sector = "s", Country = "JO", ContactName = "c", ContactEmail = "o@test.local", PhoneNumber = "0", Password = "SECRET-PW", PasswordResetToken = "SECRET-TOKEN", UserId = "u1" };
            _db.Organizations.Add(org);
            _db.Entry(org).Property(nameof(Organization.Location)).CurrentValue = "";
            var field = _db.Fields.First(f => f.Key == "environment");
            _db.Opportunities.Add(new Opportunity
            {
                Id = 1, TitleAr = "منحة", TitleEn = "t", TitleFr = "t",
                DescriptionAr = "x", DescriptionEn = "x", DescriptionFr = "x", DetailsAr = "x", DetailsEn = "x", DetailsFr = "x",
                EligibilityCriteriaAr = "x", EligibilityCriteriaEn = "x", EligibilityCriteriaFr = "x", BenefitsAr = "x", BenefitsEn = "x", BenefitsFr = "x",
                ApplyLink = "https://example.test", ImagePath = "x", PublishedBy = "x", CreatedByUserId = "u1",
                AvailableCountries = new List<Country> { _db.Countries.First(c => c.Id == 15) },
                Fields = new List<Field> { field },
                FundingType = FundingType.FullyFunded
            });
            _db.OpportunityDailyStats.Add(new OpportunityDailyStat { OpportunityId = 1, Day = new DateOnly(2026, 11, 4), ApplyClicks = 3 });
            _db.SaveChanges();
        }

        private BackupExporter Exporter(int keepDays = 14) => new(_db, _folder, keepDays, NullLogger<BackupExporter>.Instance);

        private static JsonDocument ReadBackup(string path)
        {
            using var fs = File.OpenRead(path);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            gz.CopyTo(ms);
            return JsonDocument.Parse(ms.ToArray());
        }

        [Fact]
        public async Task Export_WritesEveryTable_WithoutSecrets()
        {
            var result = await Exporter().ExportAsync();

            var path = Path.Combine(_folder, result.FileName);
            Assert.True(File.Exists(path));
            Assert.Matches(@"^backup-\d{4}-\d{2}-\d{2}-\d{4}\.json\.gz$", result.FileName);
            Assert.True(BackupExporter.IsValidFileName(result.FileName));

            using var doc = ReadBackup(path);
            var root = doc.RootElement;
            Assert.Equal(1, root.GetProperty("Opportunities").GetArrayLength());
            var opp = root.GetProperty("Opportunities")[0];
            Assert.Equal("منحة", opp.GetProperty("TitleAr").GetString());
            Assert.Equal((int)FundingType.FullyFunded, opp.GetProperty("FundingType").GetInt32());
            Assert.False(opp.TryGetProperty("IsExpired", out _));          // محسوبة، ليست عمودًا
            Assert.False(opp.TryGetProperty("AvailableCountries", out _)); // تنقّل

            var user = root.GetProperty("AspNetUsers")[0];
            Assert.Equal("u1@test.local", user.GetProperty("Email").GetString());
            Assert.False(user.TryGetProperty("PasswordHash", out _));
            Assert.False(user.TryGetProperty("SecurityStamp", out _));

            var org = root.GetProperty("Organizations")[0];
            Assert.Equal("Org", org.GetProperty("Name").GetString());
            Assert.False(org.TryGetProperty("Password", out _));
            Assert.False(org.TryGetProperty("PasswordResetToken", out _));

            Assert.False(root.TryGetProperty("AspNetUserTokens", out _));
            Assert.Equal(1, root.GetProperty("OpportunityFields").GetArrayLength()); // جدول ربط مشترك
            Assert.Equal(25, root.GetProperty("Fields").GetArrayLength());
            Assert.Equal(24, root.GetProperty("Countries").GetArrayLength());
            Assert.Equal("2026-11-04", root.GetProperty("OpportunityDailyStats")[0].GetProperty("Day").GetString());
            Assert.DoesNotContain("SECRET", File.ReadAllText(path).Length > 0 ? Decompress(path) : "");
            Assert.True(result.Counts["Opportunities"] == 1 && result.Counts.Count > 10);
        }

        [Fact]
        public async Task Export_PrunesFilesOlderThanKeepDays_AndListsNewestFirst()
        {
            Directory.CreateDirectory(_folder);
            var old = Path.Combine(_folder, "backup-2020-01-01-0300.json.gz");
            File.WriteAllBytes(old, new byte[] { 1 });
            File.SetLastWriteTime(old, DateTime.Now.AddDays(-30));
            var recent = Path.Combine(_folder, "backup-2025-01-01-0300.json.gz");
            File.WriteAllBytes(recent, new byte[] { 1 });
            File.SetLastWriteTime(recent, DateTime.Now.AddDays(-2));

            var exporter = Exporter(keepDays: 14);
            await exporter.ExportAsync();

            Assert.False(File.Exists(old));
            Assert.True(File.Exists(recent));
            var list = exporter.List();
            Assert.Equal(2, list.Count);
            Assert.StartsWith("backup-20", list[0].Name);
            Assert.True(string.CompareOrdinal(list[0].Name, list[1].Name) > 0);
            Assert.True(exporter.HasBackupFor(DateOnly.FromDateTime(DateTime.Now)));
            Assert.False(exporter.HasBackupFor(new DateOnly(2019, 1, 1)));
        }

        [Theory]
        [InlineData("backup-2026-11-04-0300.json.gz", true)]
        [InlineData("../appsettings.json", false)]
        [InlineData("backup-2026-11-04-0300.json.gz/../x", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void FileNameValidation(string? name, bool valid)
        {
            Assert.Equal(valid, BackupExporter.IsValidFileName(name));
        }

        private static string Decompress(string path)
        {
            using var fs = File.OpenRead(path);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var sr = new StreamReader(gz);
            return sr.ReadToEnd();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
            try { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }
}
