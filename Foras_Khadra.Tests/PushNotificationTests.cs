using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Foras_Khadra.Data;
using Foras_Khadra.Helpers;
using Foras_Khadra.Models;
using Foras_Khadra.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foras_Khadra.Tests
{
    /// <summary>تشفير Web Push (RFC 8291) وتوقيع VAPID (RFC 8292) والتحقق من روابط خدمات الدفع.</summary>
    public class WebPushCryptoTests
    {
        private static byte[] B64(string s) => WebEncoders.Base64UrlDecode(s);

        [Fact]
        public void Encrypt_MatchesRfc8291AppendixA()
        {
            var plaintext = B64("V2hlbiBJIGdyb3cgdXAsIEkgd2FudCB0byBiZSBhIHdhdGVybWVsb24");
            var asPublic = B64("BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8");
            using var serverKey = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = B64("yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw"),
                Q = new ECPoint { X = asPublic[1..33], Y = asPublic[33..65] }
            });

            var body = WebPushSender.Encrypt(plaintext,
                B64("BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4"),
                B64("BTBZMqHH6r4Tts7J_aSIgg"),
                serverKey,
                B64("DGv6ra1nlYgDCS1FRnbzlw"));

            Assert.Equal(
                "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPTpK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN",
                WebEncoders.Base64UrlEncode(body));
        }

        [Fact]
        public void Encrypt_RoundTripsWithBrowserKey()
        {
            using var browserKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var p = browserKey.ExportParameters(false);
            byte[] uaPublic = [0x04, .. p.Q.X!, .. p.Q.Y!];
            var auth = RandomNumberGenerator.GetBytes(16);
            var message = "{\"title\":\"⏰ باقي 3 أيام\",\"url\":\"/Opportunity/Saved\"}";

            var body = WebPushSender.Encrypt(Encoding.UTF8.GetBytes(message), uaPublic, auth);

            // فك التشفير كما يفعل المتصفح: الترويسة ثم المفتاح المشترك ثم AES-GCM
            var salt = body[..16];
            Assert.Equal(4096u, BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(16)));
            int idLength = body[20];
            var asPublic = body[21..(21 + idLength)];
            var record = body[(21 + idLength)..];
            using var serverPublic = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = asPublic[1..33], Y = asPublic[33..65] }
            });
            var (cek, nonce) = WebPushSender.DeriveKeys(browserKey, serverPublic.PublicKey, auth, salt, uaPublic, asPublic);
            var plain = new byte[record.Length - 16];
            using (var aes = new AesGcm(cek, 16))
                aes.Decrypt(nonce, record[..^16], record[^16..], plain);

            Assert.Equal(0x02, plain[^1]);
            Assert.Equal(message, Encoding.UTF8.GetString(plain[..^1]));
        }

        [Theory]
        [InlineData("https://fcm.googleapis.com/fcm/send/abc", true)]
        [InlineData("https://web.push.apple.com/QGh1", true)]
        [InlineData("https://updates.push.services.mozilla.com/wpush/v2/x", true)]
        [InlineData("https://wns2-par02p.notify.windows.com/w/?token=x", true)]
        [InlineData("http://fcm.googleapis.com/fcm/send/abc", false)]          // ليس https
        [InlineData("https://fcm.googleapis.com:8443/fcm/send/abc", false)]    // منفذ غير قياسي
        [InlineData("https://evil-fcm.googleapis.com.attacker.test/x", false)]
        [InlineData("https://localhost/x", false)]
        [InlineData("https://10.0.0.5/x", false)]
        [InlineData("not a url", false)]
        [InlineData(null, false)]
        public void PushEndpoints_OnlyKnownPushServices(string? endpoint, bool allowed) =>
            Assert.Equal(allowed, PushEndpoints.IsAllowed(endpoint));

        [Fact]
        public void PushEndpoints_ValidatesKeyLengths()
        {
            using var key = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var p = key.ExportParameters(false);
            var p256dh = WebEncoders.Base64UrlEncode([0x04, .. p.Q.X!, .. p.Q.Y!]);
            var auth = WebEncoders.Base64UrlEncode(new byte[16]);

            Assert.True(PushEndpoints.KeysAreValid(p256dh, auth));
            Assert.False(PushEndpoints.KeysAreValid(p256dh, WebEncoders.Base64UrlEncode(new byte[8])));
            Assert.False(PushEndpoints.KeysAreValid("!!not-base64!!", auth));
            Assert.False(PushEndpoints.KeysAreValid(null, auth));
        }

        [Fact]
        public void VapidKeyStore_PersistsKeysAndSignsVerifiableJwt()
        {
            var path = Path.Combine(Path.GetTempPath(), "fk-vapid-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                string publicKey, jwt;
                using (var first = new VapidKeyStore(path))
                {
                    publicKey = first.PublicKey;
                    jwt = first.CreateJwt("https://fcm.googleapis.com");
                }
                using (var second = new VapidKeyStore(path))
                    Assert.Equal(publicKey, second.PublicKey); // نفس المفتاح بعد إعادة التشغيل

                var parts = jwt.Split('.');
                Assert.Equal(3, parts.Length);
                var claims = JsonDocument.Parse(WebEncoders.Base64UrlDecode(parts[1])).RootElement;
                Assert.Equal("https://fcm.googleapis.com", claims.GetProperty("aud").GetString());
                Assert.Equal(VapidKeyStore.Subject, claims.GetProperty("sub").GetString());
                Assert.True(claims.GetProperty("exp").GetInt64() > DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                var pub = WebEncoders.Base64UrlDecode(publicKey);
                using var verifier = ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint { X = pub[1..33], Y = pub[33..65] }
                });
                Assert.True(verifier.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]),
                    WebEncoders.Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>العدّ التنازلي، تقسيم "فرصي"، ونصوص إشعارات الدفع.</summary>
    public class MyOpportunitiesTests
    {
        private static readonly DateTime Today = new(2026, 10, 29);

        private static Opportunity Opp(int id, int? daysLeft, DeadlineType type = DeadlineType.SpecificDate) => new()
        {
            Id = id,
            TitleAr = "فرصة " + id, TitleEn = "Opp " + id, TitleFr = "Opp " + id,
            DeadlineType = type,
            EndDate = daysLeft.HasValue ? Today.AddDays(daysLeft.Value) : null
        };

        [Theory]
        [InlineData(-1, "انتهى الموعد", "dl-expired")]
        [InlineData(0, "آخر يوم اليوم!", "dl-today")]
        [InlineData(1, "ينتهي غدًا", "dl-today")]
        [InlineData(2, "باقي يومان", "dl-soon")]
        [InlineData(5, "باقي 5 أيام", "dl-soon")]
        [InlineData(9, "باقي 9 أيام", "dl-ok")]
        [InlineData(20, "باقي 20 يومًا", "dl-ok")]
        public void DeadlineInfo_ArabicCountdown(int days, string label, string css)
        {
            var info = DeadlineInfo.For(Opp(1, days), "ar", Today);
            Assert.Equal(label, info.Label);
            Assert.Equal(css, info.CssClass);
        }

        [Fact]
        public void DeadlineInfo_OpenDeadlines()
        {
            var full = DeadlineInfo.For(Opp(1, null, DeadlineType.UntilFull), "en", Today);
            Assert.Null(full.DaysLeft);
            Assert.Equal("Until full", full.Label);
            Assert.False(full.IsUrgent);
            Assert.Equal("dl-open", DeadlineInfo.For(Opp(2, null, DeadlineType.NotSpecified), "ar", Today).CssClass);
        }

        [Fact]
        public void Build_GroupsByWhatNeedsDoing()
        {
            var opps = new[] { Opp(1, 20), Opp(2, 3), Opp(3, 1), Opp(4, -5), Opp(5, 10), Opp(6, 2), Opp(7, null, DeadlineType.UntilFull) };
            var cards = new Dictionary<int, TrackerCard>
            {
                [5] = new(5, ApplicationStatus.Applied, 0, 0),
                [6] = new(6, ApplicationStatus.Accepted, 0, 0)
            };

            var model = MyOpportunitiesViewModel.Build(opps, cards, "ar", Today);

            Assert.Equal(new[] { 3, 2 }, model.Urgent.Select(i => i.Opportunity.Id));      // الأقرب أولًا
            Assert.Equal(new[] { 1, 7 }, model.Preparing.Select(i => i.Opportunity.Id));   // المفتوح في الآخر
            Assert.Equal(new[] { 5 }, model.Applied.Select(i => i.Opportunity.Id));
            Assert.Equal(new[] { 6 }, model.Results.Select(i => i.Opportunity.Id));        // المقبول ليس "عاجلًا" رغم قرب موعده
            Assert.Equal(new[] { 4 }, model.Expired.Select(i => i.Opportunity.Id));
            Assert.Equal(7, model.Total);
            Assert.Equal(2, model.AppliedCount);
            Assert.Equal(1, model.AcceptedCount);
            Assert.Equal(new[] { 3, 2, 1, 7 }, model.UpNext.Select(i => i.Opportunity.Id));
        }

        [Fact]
        public void NotificationText_DeadlineLinksToOpportunity()
        {
            var opp = Opp(42, 3);
            var n = new Notification { Id = 9, Kind = NotificationKind.SavedDeadlineSoon, OpportunityId = 42, Opportunity = opp };

            var msg = NotificationText.For(n, "ar", Today)!;

            Assert.Equal("⏰ باقي 3 أيام", msg.Title);
            Assert.Contains("فرصة 42", msg.Body);
            Assert.StartsWith("/Opportunity/Details/42/", msg.Url);
            Assert.Equal("opp42", msg.Tag);
            var json = JsonDocument.Parse(msg.ToJson()).RootElement;
            Assert.Equal("rtl", json.GetProperty("dir").GetString());
        }

        [Fact]
        public void NotificationText_SkipsDeadlineWithoutOpportunity() =>
            Assert.Null(NotificationText.For(new Notification { Kind = NotificationKind.SavedDeadlineLastDay }, "en"));
    }

    /// <summary>PushDispatcher على قاعدة SQLite في الذاكرة بمرسل وهمي.</summary>
    public class PushDispatcherTests : IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly ApplicationDbContext _db;
        private readonly FakeSender _sender = new();
        private readonly PushDispatcher _dispatcher;

        private sealed class FakeSender : IWebPushSender
        {
            public List<(string Endpoint, string Payload)> Sent { get; } = new();
            public HashSet<string> GoneEndpoints { get; } = new();

            public Task<PushResult> SendAsync(PushSubscription subscription, string payloadJson, CancellationToken ct = default)
            {
                if (GoneEndpoints.Contains(subscription.Endpoint)) return Task.FromResult(PushResult.Gone);
                Sent.Add((subscription.Endpoint, payloadJson));
                return Task.FromResult(PushResult.Sent);
            }
        }

        public PushDispatcherTests()
        {
            _connection.Open();
            _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
            _dispatcher = new PushDispatcher(_db, _sender);

            _db.Users.AddRange(
                new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.local" },
                new ApplicationUser { Id = "u2", UserName = "u2", Email = "u2@test.local" });
            _db.SaveChanges();
        }

        /// <summary>فرق توقيت يجعل الساعة الآن عند المشترك = hour (للتحكم في ساعات الهدوء).</summary>
        private static int OffsetForLocalHour(int hour)
        {
            var utcHour = DateTime.UtcNow.Hour;
            return (utcHour - hour) * 60;
        }

        private PushSubscription Subscribe(string userId, string endpoint, int localHour = 12)
        {
            var s = new PushSubscription
            {
                UserId = userId, Endpoint = endpoint, P256dh = "k", Auth = "a", Lang = "ar",
                TzOffsetMinutes = OffsetForLocalHour(localHour)
            };
            _db.PushSubscriptions.Add(s);
            _db.SaveChanges();
            return s;
        }

        private Notification Notify(string userId, int count = 3, DateTime? createdAt = null)
        {
            var n = new Notification { UserId = userId, Kind = NotificationKind.InterestMatches, Count = count, CreatedAt = createdAt ?? DateTime.Now };
            _db.Notifications.Add(n);
            _db.SaveChanges();
            return n;
        }

        [Fact]
        public async Task SendsToEveryDeviceOnce_AndMarksPushed()
        {
            Subscribe("u1", "https://fcm.googleapis.com/a");
            Subscribe("u1", "https://web.push.apple.com/b");
            var n = Notify("u1");

            Assert.Equal(1, await _dispatcher.DispatchPendingAsync());
            Assert.Equal(2, _sender.Sent.Count);
            Assert.Contains("فرص جديدة لك", _sender.Sent[0].Payload);
            Assert.NotNull((await _db.Notifications.FindAsync(n.Id))!.PushedAt);

            Assert.Equal(0, await _dispatcher.DispatchPendingAsync()); // لا تكرار
            Assert.Equal(2, _sender.Sent.Count);
        }

        [Fact]
        public async Task UsersWithoutDevices_AreMarkedWithoutSending()
        {
            var n = Notify("u2");

            await _dispatcher.DispatchPendingAsync();

            Assert.Empty(_sender.Sent);
            Assert.NotNull((await _db.Notifications.FindAsync(n.Id))!.PushedAt);
        }

        [Fact]
        public async Task QuietHours_DeferUntilMorning()
        {
            var sub = Subscribe("u1", "https://fcm.googleapis.com/a", localHour: 3);
            var n = Notify("u1");

            await _dispatcher.DispatchPendingAsync();
            Assert.Empty(_sender.Sent);
            Assert.Null((await _db.Notifications.FindAsync(n.Id))!.PushedAt); // مؤجَّل لا مُسقَط

            sub.TzOffsetMinutes = OffsetForLocalHour(9);
            await _db.SaveChangesAsync();
            await _dispatcher.DispatchPendingAsync();
            Assert.Single(_sender.Sent);
        }

        [Fact]
        public async Task StaleNotifications_AreSkipped()
        {
            Subscribe("u1", "https://fcm.googleapis.com/a");
            var n = Notify("u1", createdAt: DateTime.Now.AddHours(-PushDispatcher.MaxAgeHours - 1));

            await _dispatcher.DispatchPendingAsync();

            Assert.Empty(_sender.Sent);
            Assert.NotNull((await _db.Notifications.FindAsync(n.Id))!.PushedAt);
        }

        [Fact]
        public async Task GoneSubscriptions_AreDeleted()
        {
            Subscribe("u1", "https://fcm.googleapis.com/old");
            Subscribe("u1", "https://fcm.googleapis.com/new");
            _sender.GoneEndpoints.Add("https://fcm.googleapis.com/old");
            Notify("u1");

            await _dispatcher.DispatchPendingAsync();

            Assert.Equal("https://fcm.googleapis.com/new", Assert.Single(_db.PushSubscriptions).Endpoint);
            Assert.Single(_sender.Sent);
        }

        [Fact]
        public async Task CapsPushesPerUserPerRun()
        {
            Subscribe("u1", "https://fcm.googleapis.com/a");
            for (var i = 0; i < PushDispatcher.MaxPerUserPerRun + 2; i++) Notify("u1", i + 1);

            await _dispatcher.DispatchPendingAsync();

            Assert.Equal(PushDispatcher.MaxPerUserPerRun, _sender.Sent.Count);
            Assert.All(_db.Notifications, n => Assert.NotNull(n.PushedAt)); // الباقي يبقى في الجرس فقط
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }
}
