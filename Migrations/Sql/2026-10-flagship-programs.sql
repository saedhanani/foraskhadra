-- المشاريع الرائدة (RCOY، قمة صنّاع المحتوى، برنامج بناء القدرات) وربط النشاطات بها.
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل.
-- المحتوى الأولي للمشاريع الثلاثة يزرعه التطبيق تلقائيًا عند أول تشغيل إذا كان الجدول فارغًا (Data/FlagshipProgramSeeder).
-- بديل عن: dotnet ef database update (الهجرة 20261028191535_FlagshipPrograms)
IF OBJECT_ID('FlagshipPrograms', 'U') IS NULL
BEGIN
    CREATE TABLE FlagshipPrograms (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FlagshipPrograms PRIMARY KEY,
        Slug               nvarchar(450) NOT NULL,
        SortOrder          int NOT NULL,
        IsPublished        bit NOT NULL,
        MenuTitleAr        nvarchar(max) NOT NULL,
        MenuTitleEn        nvarchar(max) NOT NULL,
        MenuTitleFr        nvarchar(max) NOT NULL,
        TitleAr            nvarchar(max) NOT NULL,
        TitleEn            nvarchar(max) NOT NULL,
        TitleFr            nvarchar(max) NOT NULL,
        SubtitleAr         nvarchar(max) NULL,
        SubtitleEn         nvarchar(max) NULL,
        SubtitleFr         nvarchar(max) NULL,
        HeadlineValue      nvarchar(max) NULL,
        HeadlineLabelAr    nvarchar(max) NULL,
        HeadlineLabelEn    nvarchar(max) NULL,
        HeadlineLabelFr    nvarchar(max) NULL,
        IntroAr            nvarchar(max) NOT NULL,
        IntroEn            nvarchar(max) NOT NULL,
        IntroFr            nvarchar(max) NOT NULL,
        OutcomesHeadingAr  nvarchar(max) NULL,
        OutcomesHeadingEn  nvarchar(max) NULL,
        OutcomesHeadingFr  nvarchar(max) NULL,
        OutcomesAr         nvarchar(max) NULL,
        OutcomesEn         nvarchar(max) NULL,
        OutcomesFr         nvarchar(max) NULL,
        PartnersAr         nvarchar(max) NULL,
        PartnersEn         nvarchar(max) NULL,
        PartnersFr         nvarchar(max) NULL,
        HeroImagePath      nvarchar(max) NULL,
        ExternalLink       nvarchar(max) NULL,
        StatsJson          nvarchar(max) NOT NULL,
        TimelineJson       nvarchar(max) NOT NULL,
        GalleryJson        nvarchar(max) NOT NULL,
        CreatedAt          datetime2 NOT NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FlagshipPrograms_Slug' AND object_id = OBJECT_ID('FlagshipPrograms'))
    EXEC('CREATE UNIQUE INDEX IX_FlagshipPrograms_Slug ON FlagshipPrograms (Slug)');

IF COL_LENGTH('Activities', 'ProgramId') IS NULL
    ALTER TABLE Activities ADD ProgramId int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Activities_ProgramId' AND object_id = OBJECT_ID('Activities'))
    EXEC('CREATE INDEX IX_Activities_ProgramId ON Activities (ProgramId)');

-- حذف المشروع يفكّ ربط نشاطاته (SET NULL) ولا يحذفها
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Activities_FlagshipPrograms_ProgramId')
    EXEC('ALTER TABLE Activities ADD CONSTRAINT FK_Activities_FlagshipPrograms_ProgramId FOREIGN KEY (ProgramId) REFERENCES FlagshipPrograms (Id) ON DELETE SET NULL');

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261028191535_FlagshipPrograms')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261028191535_FlagshipPrograms'', ''8.0.8'')');

-- للتحقق
SELECT OBJECT_ID('FlagshipPrograms', 'U') AS programs_table, COL_LENGTH('Activities', 'ProgramId') AS activities_program_column;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
