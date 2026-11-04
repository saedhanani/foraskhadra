-- تحليلات داخلية بلا كوكيز (المرحلة 1.6 من خطة 2026): ثلاثة جداول يومية مجمَّعة (Services/Analytics).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261104140311_Analytics)
-- OpportunityDailyStats.Views (موجود منذ المرحلة 0) يبدأ بالامتلاء بمشاهدات صفحات الفرص.

IF OBJECT_ID('PageViewDailyStats', 'U') IS NULL
BEGIN
    CREATE TABLE PageViewDailyStats (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PageViewDailyStats PRIMARY KEY,
        Day date NOT NULL,
        Path nvarchar(200) NOT NULL,
        Culture nvarchar(2) NOT NULL,
        Views int NOT NULL
    );
    CREATE UNIQUE INDEX IX_PageViewDailyStats_Day_Path_Culture ON PageViewDailyStats (Day, Path, Culture);
END;

IF OBJECT_ID('TrafficSourceDailyStats', 'U') IS NULL
BEGIN
    CREATE TABLE TrafficSourceDailyStats (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TrafficSourceDailyStats PRIMARY KEY,
        Day date NOT NULL,
        Source nvarchar(100) NOT NULL,
        Views int NOT NULL
    );
    CREATE UNIQUE INDEX IX_TrafficSourceDailyStats_Day_Source ON TrafficSourceDailyStats (Day, Source);
END;

IF OBJECT_ID('SiteEventDailyStats', 'U') IS NULL
BEGIN
    CREATE TABLE SiteEventDailyStats (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SiteEventDailyStats PRIMARY KEY,
        Day date NOT NULL,
        Event nvarchar(60) NOT NULL,
        Label nvarchar(200) NULL,
        Count int NOT NULL
    );
    CREATE UNIQUE INDEX IX_SiteEventDailyStats_Day_Event_Label ON SiteEventDailyStats (Day, Event, Label) WHERE Label IS NOT NULL;
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104140311_Analytics')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261104140311_Analytics', '10.0.14');

-- للتحقق
SELECT OBJECT_ID('PageViewDailyStats', 'U') AS has_page_views, OBJECT_ID('TrafficSourceDailyStats', 'U') AS has_sources, OBJECT_ID('SiteEventDailyStats', 'U') AS has_events;
