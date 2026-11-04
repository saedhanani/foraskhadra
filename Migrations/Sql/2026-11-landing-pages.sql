-- صفحات النية (المرحلة 2.3 من خطة 2026): جدول LandingPages (slug ثابت، نصوص ×3 لغات، فلتر JSON).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261104161802_LandingPages)
-- المحتوى لا يُدرج هنا: بعد النشر اضغط «استيراد الدفعة الأولى» في /Admin/LandingPages (36 صفحة، الموجود بالـslug لا يُمسّ).

IF OBJECT_ID('LandingPages', 'U') IS NULL
BEGIN
    CREATE TABLE LandingPages (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LandingPages PRIMARY KEY,
        Slug nvarchar(80) NOT NULL,
        TitleAr nvarchar(200) NOT NULL,
        TitleEn nvarchar(200) NOT NULL,
        TitleFr nvarchar(200) NOT NULL,
        IntroAr nvarchar(max) NULL,
        IntroEn nvarchar(max) NULL,
        IntroFr nvarchar(max) NULL,
        FaqJson nvarchar(max) NULL,
        FilterJson nvarchar(max) NOT NULL,
        IsPublished bit NOT NULL,
        SortOrder int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX IX_LandingPages_Slug ON LandingPages (Slug);
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104161802_LandingPages')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261104161802_LandingPages', '10.0.14');

-- للتحقق
SELECT OBJECT_ID('LandingPages', 'U') AS has_landing_pages;
