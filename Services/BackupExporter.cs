using System.IO.Compression;
using System.Text.Json;
using Foras_Khadra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foras_Khadra.Services
{
    /// <summary>
    /// نسخة احتياطية يومية من قاعدة البيانات إلى App_Data/backups/backup-YYYY-MM-DD-HHmm.json.gz (المرحلة 0.10 من خطة 2026).
    /// تمرّ على كل كيانات نموذج EF (بما فيها جداول الربط وجداول Identity) وتكتب الأعمدة المخزَّنة فقط — لا تنقّلات ولا خصائص محسوبة —
    /// مع استثناء كلمات المرور ورموز الاستعادة. ليست بديلًا عن النسخ الكامل من لوحة الاستضافة، بل شبكة أمان يومية بلا تدخل.
    /// </summary>
    public interface IBackupExporter
    {
        string Folder { get; }
        Task<BackupResult> ExportAsync(CancellationToken ct = default);
        IReadOnlyList<BackupFileInfo> List();
        bool HasBackupFor(DateOnly day);
    }

    public sealed record BackupFileInfo(string Name, long Bytes, DateTime CreatedAt);
    public sealed record BackupResult(string FileName, long Bytes, IReadOnlyDictionary<string, int> Counts);

    public class BackupExporter : IBackupExporter
    {
        public const int DefaultKeepDays = 14;
        public const string FilePrefix = "backup-";

        /// <summary>أعمدة لا تُصدَّر أبدًا (أسرار الحسابات).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> ExcludedColumns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["AspNetUsers"] = new[] { "PasswordHash", "SecurityStamp", "ConcurrencyStamp" },
            ["Organizations"] = new[] { "Password", "PasswordResetToken", "TokenExpiry" },
        };

        /// <summary>جداول لا تُصدَّر (رموز جلسات/مصادقة).</summary>
        public static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase) { "AspNetUserTokens" };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<BackupExporter> _log;
        private readonly int _keepDays;

        public string Folder { get; }

        public BackupExporter(ApplicationDbContext db, IHostEnvironment env, IConfiguration cfg, ILogger<BackupExporter> log)
            : this(db, Path.Combine(env.ContentRootPath, "App_Data", "backups"), cfg.GetValue<int?>("Backup:KeepDays") ?? DefaultKeepDays, log) { }

        /// <summary>للاختبار: مجلد صريح.</summary>
        public BackupExporter(ApplicationDbContext db, string folder, int keepDays, ILogger<BackupExporter> log)
        {
            _db = db;
            Folder = folder;
            _keepDays = keepDays;
            _log = log;
        }

        public async Task<BackupResult> ExportAsync(CancellationToken ct = default)
        {
            Directory.CreateDirectory(Folder);
            var now = DateTime.Now;
            var fileName = $"{FilePrefix}{now:yyyy-MM-dd-HHmm}.json.gz";
            var finalPath = Path.Combine(Folder, fileName);
            var tmpPath = finalPath + ".tmp";
            var counts = new Dictionary<string, int>();

            await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            await using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
            await using (var w = new Utf8JsonWriter(gz))
            {
                w.WriteStartObject();
                w.WriteString("exportedAt", now.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                w.WriteString("schema", "ef-model-v1");

                foreach (var entityType in _db.Model.GetEntityTypes().Where(e => !e.IsOwned()).OrderBy(e => e.GetTableName()))
                {
                    ct.ThrowIfCancellationRequested();
                    var table = entityType.GetTableName() ?? entityType.ShortName();
                    if (ExcludedTables.Contains(table)) continue;

                    var excluded = ExcludedColumns.TryGetValue(table, out var cols) ? new HashSet<string>(cols, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
                    var props = entityType.GetProperties().Where(p => !excluded.Contains(p.Name)).ToList();

                    var rows = await LoadRowsAsync(entityType, ct);
                    w.WritePropertyName(table);
                    w.WriteStartArray();
                    foreach (var row in rows)
                    {
                        w.WriteStartObject();
                        foreach (var p in props)
                        {
                            w.WritePropertyName(p.Name);
                            var value = ReadValue(row, p);
                            if (value == null) w.WriteNullValue();
                            else JsonSerializer.Serialize(w, value, value.GetType(), JsonOptions);
                        }
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    counts[table] = rows.Count;
                }
                w.WriteEndObject();
                await w.FlushAsync(ct);
            }

            File.Move(tmpPath, finalPath, overwrite: true);
            Prune();
            var bytes = new FileInfo(finalPath).Length;
            _log.LogInformation("Backup written: {File} ({Bytes} bytes, {Tables} tables)", fileName, bytes, counts.Count);
            return new BackupResult(fileName, bytes, counts);
        }

        public IReadOnlyList<BackupFileInfo> List()
        {
            if (!Directory.Exists(Folder)) return Array.Empty<BackupFileInfo>();
            return new DirectoryInfo(Folder).GetFiles(FilePrefix + "*.json.gz")
                .OrderByDescending(f => f.Name)
                .Select(f => new BackupFileInfo(f.Name, f.Length, f.LastWriteTime))
                .ToList();
        }

        public bool HasBackupFor(DateOnly day) =>
            Directory.Exists(Folder) && Directory.EnumerateFiles(Folder, $"{FilePrefix}{day:yyyy-MM-dd}-*.json.gz").Any();

        /// <summary>اسم ملف نسخة صالح (يمنع المسارات في طلب التنزيل).</summary>
        public static bool IsValidFileName(string? name) =>
            !string.IsNullOrEmpty(name) && System.Text.RegularExpressions.Regex.IsMatch(name, @"^backup-\d{4}-\d{2}-\d{2}-\d{4}\.json\.gz$");

        private void Prune()
        {
            var cutoff = DateTime.Now.AddDays(-_keepDays);
            foreach (var f in new DirectoryInfo(Folder).GetFiles(FilePrefix + "*.json.gz"))
            {
                if (f.LastWriteTime >= cutoff) continue;
                try { f.Delete(); }
                catch (IOException ex) { _log.LogWarning(ex, "Could not delete old backup {File}", f.Name); }
            }
            foreach (var tmp in new DirectoryInfo(Folder).GetFiles("*.tmp"))
            {
                try { tmp.Delete(); } catch (IOException) { }
            }
        }

        private async Task<List<object>> LoadRowsAsync(IEntityType entityType, CancellationToken ct)
        {
            // كيانات الربط المشتركة (OpportunityFields, CountryOpportunity) قاموس؛ البقية أصناف CLR
            if (entityType.ClrType == typeof(Dictionary<string, object>))
            {
                var set = _db.Set<Dictionary<string, object>>(entityType.Name);
                return (await set.AsNoTracking().ToListAsync(ct)).Cast<object>().ToList();
            }
            var method = typeof(BackupExporter).GetMethod(nameof(LoadTypedAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            return await (Task<List<object>>)method.Invoke(this, new object[] { ct })!;
        }

        private async Task<List<object>> LoadTypedAsync<T>(CancellationToken ct) where T : class =>
            (await _db.Set<T>().AsNoTracking().ToListAsync(ct)).Cast<object>().ToList();

        private static object? ReadValue(object row, IProperty p)
        {
            if (row is Dictionary<string, object> dict) return dict.TryGetValue(p.Name, out var v) ? v : null;
            if (p.PropertyInfo != null) return p.PropertyInfo.GetValue(row);
            if (p.FieldInfo != null) return p.FieldInfo.GetValue(row);
            return null; // خاصية ظل بلا عضو CLR
        }
    }
}
