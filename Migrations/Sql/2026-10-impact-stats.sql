-- أرقام الأثر في بانر صفحة "نشاطاتنا" (تُحرَّر من لوحة التحكم). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. الأرقام الستة الأولية يزرعها التطبيق تلقائيًا عند أول تشغيل إذا كان الجدول فارغًا.
-- بديل عن: dotnet ef database update (الهجرة 20261028193319_ImpactStats)
IF OBJECT_ID('ImpactStats', 'U') IS NULL
BEGIN
    CREATE TABLE ImpactStats (
        Id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ImpactStats PRIMARY KEY,
        Value     nvarchar(max) NOT NULL,
        LabelAr   nvarchar(max) NOT NULL,
        LabelEn   nvarchar(max) NOT NULL,
        LabelFr   nvarchar(max) NOT NULL,
        SortOrder int NOT NULL,
        IsVisible bit NOT NULL
    );
END

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261028193319_ImpactStats')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261028193319_ImpactStats'', ''8.0.8'')');

-- للتحقق
SELECT OBJECT_ID('ImpactStats', 'U') AS impact_stats_table;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
