// أداة النشر (المرحلة 5.3) — تطبيق C# من ملف واحد (.NET 10): يبني، ويقارن بالإصدار الحي، ويحزم المتغيّر فقط،
// ويجمع سكربتات SQL الجديدة بالترتيب، ويكتب قائمة التحقق. لا يرفع شيئًا ولا يحمل أسرارًا: الرفع يبقى من لوحة الاستضافة
// (docs/ROADMAP-2026.md §12). الاستعمال عبر الغلاف من جذر المستودع (أو `cd tools` ثم `dotnet run deploy.cs -- …`؛
// من الجذر مباشرة يشغّل dotnet مشروع الموقع لا هذا الملف):
//
//   .\tools\deploy.ps1                                   حزمة + SQL + قائمة تحقق لآخر commit (الحي من publish/site-files-current/LIVE-SHA.txt)
//   .\tools\deploy.ps1 --live a87cfef                    حين لا يوجد LIVE-SHA.txt
//   .\tools\deploy.ps1 --test                            يشغّل الاختبارات أولًا
//   .\tools\deploy.ps1 --skip-publish                    يعيد التحزيم من publish/new-<sha> الموجود
//   .\tools\deploy.ps1 --verify https://www.foraskhadra.com   يفحص الصفحات الأساسية بعد النشر
//   .\tools\deploy.ps1 --sync --sha <sha>                بعد نجاح النشر: يحدّث site-files-current ويكتب LIVE-SHA.txt
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;
var options = DeployOptions.Parse(args);
if (options.Help) { Console.WriteLine(DeployOptions.Usage); return 0; }

var root = Tool.FindRoot(Environment.CurrentDirectory);
if (root == null) { Console.Error.WriteLine("شغّل الأداة من داخل المستودع (المجلد الذي فيه Foras_Khadra.csproj)."); return 1; }
Directory.SetCurrentDirectory(root);
var publishRoot = Path.Combine(root, "publish");
var liveDir = Path.Combine(publishRoot, "site-files-current");
var liveShaFile = Path.Combine(liveDir, "LIVE-SHA.txt");

if (options.Verify != null) return await Tool.VerifyAsync(options.Verify);

var sha = options.Sha ?? Tool.Git("rev-parse --short HEAD").Trim() + (Tool.Git("status --porcelain --untracked-files=no").Trim().Length > 0 ? "-dirty" : "");
var newDir = Path.Combine(publishRoot, "new-" + sha);
var deployDir = Path.Combine(publishRoot, "deploy-" + sha);

if (options.Sync) return Tool.Sync(newDir, liveDir, deployDir, sha, liveShaFile);

var live = options.Live ?? (File.Exists(liveShaFile) ? File.ReadAllText(liveShaFile).Trim() : "");
if (live.Length == 0) { Console.Error.WriteLine("لا أعرف الإصدار الحي: مرّر --live <sha> أو اكتب publish/site-files-current/LIVE-SHA.txt"); return 1; }
if (!Directory.Exists(liveDir)) { Console.Error.WriteLine($"مجلد الإصدار الحي غير موجود: {liveDir} (انسخ ملفات الخادم إليه أولًا)"); return 1; }

Console.WriteLine($"الإصدار الحي: {live} → هذا الإصدار: {sha}");
if (sha.EndsWith("-dirty")) Console.WriteLine("تنبيه: تغييرات غير ملتزَمة في الشجرة — الحزمة لا تطابق أي commit.");

if (options.Test) Tool.Run("dotnet", "test Foras_Khadra.Tests/Foras_Khadra.Tests.csproj -c Release");
if (!options.SkipPublish) Tool.Run("dotnet", $"publish Foras_Khadra.csproj -c Release -o \"{newDir}\"");
else if (!Directory.Exists(newDir)) { Console.Error.WriteLine($"--skip-publish لكن {newDir} غير موجود"); return 1; }

Directory.CreateDirectory(deployDir);
var package = Tool.Package(newDir, liveDir, deployDir, sha);
var sql = Tool.CollectSql(live, deployDir);
File.Copy(Path.Combine(root, "tools", "app_offline.htm"), Path.Combine(deployDir, "app_offline.htm"), overwrite: true);
Tool.WriteChecklist(deployDir, sha, live, package, sql);

Console.WriteLine();
Console.WriteLine($"جاهز: {deployDir}");
Console.WriteLine($"  {package.Files.Count} ملفًا متغيرًا في {package.Zips.Count} حزمة · {sql.Count} سكربت SQL جديد · app_offline.htm · CHECKLIST.md");
if (package.Files.Count == 0) Console.WriteLine("  لا فرق عن الإصدار الحي — لا شيء يُرفع.");
return 0;

sealed record DeployOptions(string? Live, string? Sha, bool SkipPublish, bool Test, bool Sync, string? Verify, bool Help)
{
    public const string Usage = """
        dotnet run tools/deploy.cs -- [--live <sha>] [--sha <sha>] [--skip-publish] [--test] [--sync] [--verify <baseUrl>]
          --live <sha>      الإصدار الحي (الافتراضي: publish/site-files-current/LIVE-SHA.txt)
          --sha <sha>       اسم هذا الإصدار (الافتراضي: git rev-parse --short HEAD، و-dirty إن وُجدت تغييرات)
          --skip-publish    لا تبنِ من جديد؛ استعمل publish/new-<sha>
          --test            شغّل dotnet test قبل البناء
          --sync            بعد النشر: انسخ المتغيّر إلى site-files-current واكتب LIVE-SHA.txt
          --verify <url>    افحص الصفحات الأساسية على الموقع الحي
        """;

    public static DeployOptions Parse(string[] args)
    {
        string? live = null, sha = null, verify = null;
        bool skip = false, test = false, sync = false, help = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--live": live = args[++i]; break;
                case "--sha": sha = args[++i]; break;
                case "--verify": verify = args[++i]; break;
                case "--skip-publish": skip = true; break;
                case "--test": test = true; break;
                case "--sync": sync = true; break;
                case "-h": case "--help": help = true; break;
                default: Console.Error.WriteLine("خيار غير معروف: " + args[i]); help = true; break;
            }
        }
        return new DeployOptions(live, sha, skip, test, sync, verify, help);
    }
}

sealed record PackageResult(List<string> Files, List<(string Name, long Bytes, int Count)> Zips);

static class Tool
{
    const long ZipLimit = 9_500_000; // حدّ رفع لوحة الاستضافة 10 MB
    static readonly string[] SkipPrefixes =
    {
        "runtimes/osx", "runtimes/linux", "runtimes/unix", "runtimes/win-arm", "runtimes/maccatalyst",
        "runtimes/ios", "runtimes/android", "runtimes/tvos", "runtimes/browser", "runtimes/freebsd"
    };

    /// <summary>الصفحات التي تُفحص بعد كل نشر والحالة المتوقعة لكل منها.</summary>
    static readonly (string Path, int Expected)[] VerifyPaths =
    {
        ("/health", 200), ("/", 200), ("/en/", 200), ("/fr/", 200),
        ("/Home/AllOpportunities", 200), ("/opportunities/jobs", 200), ("/opportunities/closing-soon", 200),
        ("/guides", 200), ("/impact", 200), ("/for-organizations", 200), ("/feed/opportunities.xml", 200),
        ("/sitemap.xml", 200), ("/.well-known/assetlinks.json", 200),
        ("/api/team/ping", 401), ("/Admin/Dashboard", 302), ("/opportunities/not-a-page", 404)
    };

    public static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Foras_Khadra.csproj"))) return dir.FullName;
        return null;
    }

    public static string Git(string arguments) => Run("git", arguments, capture: true);

    public static string Run(string file, string arguments, bool capture = false)
    {
        Console.WriteLine($"> {file} {arguments}");
        var psi = new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = capture, UseShellExecute = false,
            StandardOutputEncoding = capture ? Encoding.UTF8 : null
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("تعذّر تشغيل " + file);
        var output = capture ? p.StandardOutput.ReadToEnd() : "";
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException($"{file} {arguments} انتهى برمز {p.ExitCode}");
        return output;
    }

    static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    static bool IsSkipped(string rel)
    {
        var low = rel.ToLowerInvariant();
        var name = Path.GetFileName(low);
        return low == "countrydata.dll" || name.StartsWith("appsettings")
            || SkipPrefixes.Any(p => low.StartsWith(p)) || (low.StartsWith("runtimes/") && low.EndsWith(".pdb"));
    }

    public static PackageResult Package(string newDir, string liveDir, string deployDir, string sha)
    {
        var changed = new List<string>();
        var skipped = 0;
        foreach (var full in Directory.EnumerateFiles(newDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(newDir, full).Replace('\\', '/');
            if (IsSkipped(rel)) { skipped++; continue; }
            var old = Path.Combine(liveDir, rel);
            if (!File.Exists(old) || new FileInfo(old).Length != new FileInfo(full).Length || Sha256(old) != Sha256(full))
                changed.Add(rel);
        }
        changed = changed.OrderBy(r => r.StartsWith("runtimes/")).ThenBy(r => r, StringComparer.Ordinal).ToList();

        // تقسيم بحجم غير مضغوط ≈ 2.2 × الحدّ (الضغط يُنزل DLL إلى نحو النصف)
        var parts = new List<List<string>>();
        var current = new List<string>();
        long size = 0;
        foreach (var rel in changed)
        {
            var bytes = new FileInfo(Path.Combine(newDir, rel)).Length;
            if (current.Count > 0 && size + bytes > ZipLimit * 2.2) { parts.Add(current); current = new List<string>(); size = 0; }
            current.Add(rel);
            size += bytes;
        }
        if (current.Count > 0) parts.Add(current);

        foreach (var stale in Directory.EnumerateFiles(deployDir, "update-*.zip")) File.Delete(stale);
        var zips = new List<(string, long, int)>();
        for (var i = 0; i < parts.Count; i++)
        {
            var name = parts.Count == 1 ? $"update-{sha}.zip" : $"update-{sha}-part{i + 1}.zip";
            var path = Path.Combine(deployDir, name);
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
                foreach (var rel in parts[i]) zip.CreateEntryFromFile(Path.Combine(newDir, rel), rel, CompressionLevel.SmallestSize); // أسماء بشرطة مائلة أمامية
            zips.Add((name, new FileInfo(path).Length, parts[i].Count));
        }

        File.WriteAllText(Path.Combine(deployDir, "changed-files.txt"), string.Join("\n", changed) + (changed.Count > 0 ? "\n" : ""), new UTF8Encoding(false));
        Console.WriteLine($"ملفات متغيرة: {changed.Count} (متجاهَلة: {skipped})");
        foreach (var rel in changed) Console.WriteLine("  " + rel);
        foreach (var (name, bytes, count) in zips)
            Console.WriteLine($"  {name}: {bytes / 1e6:0.00} MB، {count} ملفًا" + (bytes > 10_000_000 ? "  ← أكبر من 10 MB! قسّمها يدويًا" : ""));
        return new PackageResult(changed, zips);
    }

    /// <summary>سكربتات Migrations/Sql التي أُضيفت أو تغيّرت منذ الإصدار الحي (والجديدة غير المتتبَّعة)، منسوخة بالترتيب إلى deploy/sql.</summary>
    public static List<string> CollectSql(string liveSha, string deployDir)
    {
        var names = new List<string>();
        try
        {
            names.AddRange(Git($"diff --name-only --diff-filter=AM {liveSha}..HEAD -- Migrations/Sql")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"تنبيه: git لا يعرف {liveSha}؛ تُدرج السكربتات غير المتتبَّعة فقط.");
        }
        names.AddRange(Git("ls-files --others --exclude-standard -- Migrations/Sql")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        // البنية قبل البيانات: x.sql ثم x-data.sql ثم x-seed.sql
        static (string Base, int Rank) Order(string file)
        {
            var stem = Path.GetFileNameWithoutExtension(file);
            if (stem.EndsWith("-seed")) return (stem[..^5], 2);
            if (stem.EndsWith("-data")) return (stem[..^5], 1);
            return (stem, 0);
        }
        var ordered = names.Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)).Distinct()
            .OrderBy(n => Order(n).Base, StringComparer.Ordinal).ThenBy(n => Order(n).Rank).ToList();

        var sqlDir = Path.Combine(deployDir, "sql");
        if (Directory.Exists(sqlDir)) Directory.Delete(sqlDir, recursive: true);
        Directory.CreateDirectory(sqlDir);
        var copied = new List<string>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var target = $"{i + 1:00}-{Path.GetFileName(ordered[i])}";
            File.Copy(ordered[i], Path.Combine(sqlDir, target), overwrite: true);
            copied.Add(target);
            var text = File.ReadAllText(ordered[i]);
            var problems = new List<string>();
            if (Regex.IsMatch(text, @"\bEXEC(UTE)?\b", RegexOptions.IgnoreCase)) problems.Add("EXEC");
            if (text.Contains("ENDPOINT", StringComparison.OrdinalIgnoreCase)) problems.Add("ENDPOINT");
            if (Regex.IsMatch(text, @"(?m)^\s*GO\s*$")) problems.Add("GO");
            Console.WriteLine($"  SQL {target}" + (problems.Count > 0 ? $"  ← يحتوي {string.Join("/", problems)}: SQL Studio يرفضه" : ""));
        }
        return copied;
    }

    public static void WriteChecklist(string deployDir, string sha, string live, PackageResult package, List<string> sql)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# قائمة نشر `{sha}` (الإصدار الحي الآن: `{live}`) — {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## 1. قبل الرفع");
        sb.AppendLine("- [ ] نسخة احتياطية من لوحة الاستضافة (Databases → النسخ الاحتياطي → إنشاء نسخة في مجلد \\DB) وانتظر «نشط».");
        if (sql.Count == 0) sb.AppendLine("- [x] لا سكربتات SQL جديدة منذ الإصدار الحي.");
        else
        {
            sb.AppendLine("- [ ] SQL Studio → Query editor: نفّذ ملفات `sql/` بهذا الترتيب، **كل ملف في تنفيذ مستقل** (DDL ثم DML):");
            foreach (var s in sql) sb.AppendLine($"  - [ ] `sql/{s}`");
        }
        sb.AppendLine();
        sb.AppendLine("## 2. الرفع (مدير الملفات → /site1)");
        if (package.Files.Count == 0) sb.AppendLine("- [x] لا ملفات متغيرة — لا رفع.");
        else
        {
            sb.AppendLine($"- [ ] تحميل `app_offline.htm` + {string.Join(" + ", package.Zips.Select(z => $"`{z.Name}`"))} معًا (الحقل يقبل أكثر من ملف).");
            sb.AppendLine("- [ ] لكل حزمة: «فك الضغط» → الوجهة `/site1` (تحقق من **قيمة** الحقل لا من placeholder «/») → «قائمة انتظار العمل» → «تحديث» حتى النجاح → إغلاق بزرها.");
            sb.AppendLine($"- [ ] إعادة تسمية `app_offline.htm` → `app_offline-used-{DateTime.Now:yyyy-MM-dd}.txt` (يعود الموقع فورًا).");
        }
        sb.AppendLine();
        sb.AppendLine("## 3. التحقق");
        sb.AppendLine("```");
        sb.AppendLine(".\\tools\\deploy.ps1 --verify https://www.foraskhadra.com");
        sb.AppendLine("```");
        foreach (var (path, expected) in VerifyPaths) sb.AppendLine($"- `{path}` → {expected}");
        sb.AppendLine("- صفحة فرصة بكل لغة، الدخول، الدفع، وأي صفحة جديدة في docs/DEPLOY-<المرحلة>.md.");
        sb.AppendLine();
        sb.AppendLine("## 4. بعد النشر");
        sb.AppendLine("```");
        sb.AppendLine($".\\tools\\deploy.ps1 --sync --sha {sha}");
        sb.AppendLine("```");
        sb.AppendLine("- [ ] docs/ROADMAP-2026.md (الإصدار الحي) + docs/DEPLOY-<المرحلة>.md (✅ نُشر) → commit → push.");
        sb.AppendLine("- [ ] حذف الحزم القديمة وملفات `app_offline-used-*` من `/site1` عند الفرصة.");
        sb.AppendLine();
        sb.AppendLine($"## الملفات المتغيرة ({package.Files.Count})");
        foreach (var f in package.Files) sb.AppendLine("- `" + f + "`");
        File.WriteAllText(Path.Combine(deployDir, "CHECKLIST.md"), sb.ToString(), new UTF8Encoding(false));
    }

    /// <summary>بعد نجاح النشر: ينسخ المتغيّر من new-&lt;sha&gt; إلى site-files-current (بالتحقق من البصمة) ويكتب LIVE-SHA.txt.</summary>
    public static int Sync(string newDir, string liveDir, string deployDir, string sha, string liveShaFile)
    {
        var list = Path.Combine(deployDir, "changed-files.txt");
        if (!File.Exists(list)) { Console.Error.WriteLine($"لا يوجد {list} — شغّل الأداة بلا --sync أولًا."); return 1; }
        var files = File.ReadAllLines(list).Select(l => l.Trim('\r', ' ')).Where(l => l.Length > 0).ToList();
        var bad = 0;
        foreach (var rel in files)
        {
            var src = Path.Combine(newDir, rel);
            var dst = Path.Combine(liveDir, rel);
            if (!File.Exists(src)) { Console.Error.WriteLine("مفقود: " + src); bad++; continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
            if (Sha256(src) != Sha256(dst)) { Console.Error.WriteLine("بصمة مختلفة بعد النسخ: " + rel); bad++; }
        }
        if (bad > 0) return 1;
        File.WriteAllText(liveShaFile, sha, new UTF8Encoding(false));
        Console.WriteLine($"site-files-current = {sha} ({files.Count} ملفًا)");
        return 0;
    }

    public static async Task<int> VerifyAsync(string baseUrl)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("foraskhadra-deploy-monitor/1.0"); // «monitor» حتى لا تُعدّ زيارة في التحليلات
        var failures = 0;
        foreach (var (path, expected) in VerifyPaths)
        {
            int status;
            string note = "";
            try
            {
                var sw = Stopwatch.StartNew();
                using var res = await client.GetAsync(baseUrl.TrimEnd('/') + path);
                status = (int)res.StatusCode;
                note = $"{sw.ElapsedMilliseconds} ms";
                if (path == "/health" && status == 200) note += " · " + (await res.Content.ReadAsStringAsync()).Trim();
                if (res.Headers.Location != null) note += " → " + res.Headers.Location;
            }
            catch (Exception ex) { status = 0; note = ex.Message; }
            var ok = status == expected;
            if (!ok) failures++;
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {status,3} (متوقع {expected}) {path}  {note}");
        }
        Console.WriteLine(failures == 0 ? "كل الفحوص ناجحة." : $"{failures} فحصًا فشل.");
        return failures == 0 ? 0 : 1;
    }
}
