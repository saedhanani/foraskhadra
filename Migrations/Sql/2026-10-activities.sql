-- جدول نشاطات فرص خضراء (صفحة "نشاطاتنا"). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- بديل عن: dotnet ef database update (الهجرة 20261028184220_Activities)
IF OBJECT_ID('Activities', 'U') IS NULL
BEGIN
    CREATE TABLE Activities (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Activities PRIMARY KEY,
        TitleAr       nvarchar(max) NOT NULL,
        TitleEn       nvarchar(max) NOT NULL,
        TitleFr       nvarchar(max) NOT NULL,
        DescriptionAr nvarchar(max) NOT NULL,
        DescriptionEn nvarchar(max) NOT NULL,
        DescriptionFr nvarchar(max) NOT NULL,
        StartDate     datetime2 NOT NULL,
        EndDate       datetime2 NULL,
        Location      nvarchar(max) NULL,
        ExternalLink  nvarchar(max) NULL,
        ImagePath     nvarchar(max) NULL,
        IsPublished   bit NOT NULL,
        CreatedAt     datetime2 NOT NULL
    );
END

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261028184220_Activities')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261028184220_Activities'', ''8.0.8'')');

-- للتحقق
SELECT OBJECT_ID('Activities', 'U') AS activities_table;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
