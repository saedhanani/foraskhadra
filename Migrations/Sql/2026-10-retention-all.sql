-- =====================================================================
-- سكربت الإنتاج المجمّع — تحديث "العودة الدائمة" (المراحل 1–5)
-- يُنفَّذ مرة واحدة في بوابة SQL Studio قبل رفع الملفات. بلا EXEC ولا GO.
-- آمن للتكرار: كل خطوة تتحقق أولًا. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- الترتيب: الإشعارات ← تتبّع التقديم ← مراقبة الدورات ← آخر زيارة ← المجتمع
-- =====================================================================

-- ---------- 2026-10-notifications.sql ----------
-- الإشعارات الداخلية والبحوث المحفوظة (جرس الهيدر). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار: كل خطوة تتحقق أولًا. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- بلا EXEC إطلاقًا (محرر بوابة SQL Studio يرفضه).
-- بديل عن: dotnet ef database update (الهجرة 20261029131427_NotificationsAndSavedSearches)

IF COL_LENGTH('AspNetUsers', 'LastInterestNotifiedAt') IS NULL
    ALTER TABLE AspNetUsers ADD LastInterestNotifiedAt datetime2 NULL;

IF OBJECT_ID('Notifications', 'U') IS NULL
    CREATE TABLE Notifications (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
        UserId        nvarchar(450) NOT NULL,
        Kind          int NOT NULL,
        OpportunityId int NULL,
        Count         int NOT NULL,
        Label         nvarchar(max) NULL,
        LinkParams    nvarchar(max) NULL,
        CreatedAt     datetime2 NOT NULL,
        ReadAt        datetime2 NULL,
        -- بلا cascade من جهة المستخدم (SQL Server يرفض مسارين متسلسلين)؛ الحذف من جهة الفرصة متسلسل
        CONSTRAINT FK_Notifications_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_Notifications_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );

IF OBJECT_ID('SavedSearches', 'U') IS NULL
    CREATE TABLE SavedSearches (
        Id        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SavedSearches PRIMARY KEY,
        UserId    nvarchar(450) NOT NULL,
        Query     nvarchar(200) NOT NULL,
        TypesCsv  nvarchar(200) NOT NULL,
        Country   nvarchar(100) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        LastRunAt datetime2 NOT NULL,
        CONSTRAINT FK_SavedSearches_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id)
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_OpportunityId' AND object_id = OBJECT_ID('Notifications'))
    CREATE INDEX IX_Notifications_OpportunityId ON Notifications (OpportunityId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId_ReadAt' AND object_id = OBJECT_ID('Notifications'))
    CREATE INDEX IX_Notifications_UserId_ReadAt ON Notifications (UserId, ReadAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SavedSearches_UserId' AND object_id = OBJECT_ID('SavedSearches'))
    CREATE INDEX IX_SavedSearches_UserId ON SavedSearches (UserId);

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029131427_NotificationsAndSavedSearches')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029131427_NotificationsAndSavedSearches', '8.0.8');


-- ---------- 2026-10-application-tracking.sql ----------
-- المرحلة 2 من خطة العودة: تتبّع التقديم + رفيق التقديم + "تابعت مؤخرًا". ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار: كل خطوة تتحقق أولًا. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029134614_ApplicationTracking)

IF COL_LENGTH('SavedOpportunities', 'Status') IS NULL
    ALTER TABLE SavedOpportunities ADD Status int NOT NULL CONSTRAINT DF_SavedOpportunities_Status DEFAULT 0;

IF COL_LENGTH('SavedOpportunities', 'StatusChangedAt') IS NULL
    ALTER TABLE SavedOpportunities ADD StatusChangedAt datetime2 NULL;

IF OBJECT_ID('ChecklistItems', 'U') IS NULL
    CREATE TABLE ChecklistItems (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChecklistItems PRIMARY KEY,
        SavedOpportunityId int NOT NULL,
        [Key]              nvarchar(40) NULL,
        Text               nvarchar(120) NULL,
        Done               bit NOT NULL,
        SortOrder          int NOT NULL,
        CONSTRAINT FK_ChecklistItems_SavedOpportunities_SavedOpportunityId FOREIGN KEY (SavedOpportunityId) REFERENCES SavedOpportunities (Id) ON DELETE CASCADE
    );

IF OBJECT_ID('RecentViews', 'U') IS NULL
    CREATE TABLE RecentViews (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecentViews PRIMARY KEY,
        UserId        nvarchar(450) NOT NULL,
        OpportunityId int NOT NULL,
        ViewedAt      datetime2 NOT NULL,
        -- بلا cascade من جهة المستخدم (SQL Server يرفض مسارين متسلسلين)؛ الحذف من جهة الفرصة متسلسل
        CONSTRAINT FK_RecentViews_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_RecentViews_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChecklistItems_SavedOpportunityId' AND object_id = OBJECT_ID('ChecklistItems'))
    CREATE INDEX IX_ChecklistItems_SavedOpportunityId ON ChecklistItems (SavedOpportunityId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecentViews_OpportunityId' AND object_id = OBJECT_ID('RecentViews'))
    CREATE INDEX IX_RecentViews_OpportunityId ON RecentViews (OpportunityId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecentViews_UserId_OpportunityId' AND object_id = OBJECT_ID('RecentViews'))
    CREATE UNIQUE INDEX IX_RecentViews_UserId_OpportunityId ON RecentViews (UserId, OpportunityId);

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى (MigrationId يُحدَّث بعد توليد الهجرة)
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029134614_ApplicationTracking')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029134614_ApplicationTracking', '8.0.8');


-- ---------- 2026-10-cycle-watches.sql ----------
-- المرحلة 3 من خطة العودة: مراقبة الدورة القادمة ("أعلمني حين تفتح"). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- (الترشيح بالجنسية ونسبة التطابق لا يحتاجان تغييرًا في القاعدة.)
-- آمن للتكرار: كل خطوة تتحقق أولًا. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029135709_CycleWatches)

IF OBJECT_ID('CycleWatches', 'U') IS NULL
    CREATE TABLE CycleWatches (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CycleWatches PRIMARY KEY,
        UserId        nvarchar(450) NOT NULL,
        OpportunityId int NOT NULL,
        CreatedAt     datetime2 NOT NULL,
        NotifiedAt    datetime2 NULL,
        -- بلا cascade من جهة المستخدم (SQL Server يرفض مسارين متسلسلين)؛ الحذف من جهة الفرصة متسلسل
        CONSTRAINT FK_CycleWatches_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_CycleWatches_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CycleWatches_OpportunityId' AND object_id = OBJECT_ID('CycleWatches'))
    CREATE INDEX IX_CycleWatches_OpportunityId ON CycleWatches (OpportunityId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CycleWatches_UserId_OpportunityId' AND object_id = OBJECT_ID('CycleWatches'))
    CREATE UNIQUE INDEX IX_CycleWatches_UserId_OpportunityId ON CycleWatches (UserId, OpportunityId);

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029135709_CycleWatches')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029135709_CycleWatches', '8.0.8');


-- ---------- 2026-10-last-seen.sql ----------
-- المرحلة 4 من خطة العودة: "جديد منذ آخر زيارة" (عمودان على المستخدم). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- (شارات الإنجاز وعدّاد الحفظ واكتمال الملف والعدّاد التنازلي تُحسب من بيانات موجودة ولا تحتاج تغييرًا في القاعدة.)
-- آمن للتكرار. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029140651_LastSeen)

IF COL_LENGTH('AspNetUsers', 'LastSeenAt') IS NULL
    ALTER TABLE AspNetUsers ADD LastSeenAt datetime2 NULL;

IF COL_LENGTH('AspNetUsers', 'PreviousVisitAt') IS NULL
    ALTER TABLE AspNetUsers ADD PreviousVisitAt datetime2 NULL;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029140651_LastSeen')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029140651_LastSeen', '8.0.8');


-- ---------- 2026-10-community.sql ----------
-- المرحلة 5 من خطة العودة: السفراء، قوائم انتظار البرامج، خزانة الأعضاء. ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- (ملخص الأسبوع وبطاقة الإنجاز يعملان على الجداول الموجودة.)
-- آمن للتكرار. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029142324_Community)

-- 5-3 السفراء
IF COL_LENGTH('AspNetUsers', 'ReferralCode') IS NULL
    ALTER TABLE AspNetUsers ADD ReferralCode nvarchar(12) NULL;
IF COL_LENGTH('AspNetUsers', 'ReferredByUserId') IS NULL
    ALTER TABLE AspNetUsers ADD ReferredByUserId nvarchar(450) NULL;
IF COL_LENGTH('AspNetUsers', 'ReferredAt') IS NULL
    ALTER TABLE AspNetUsers ADD ReferredAt datetime2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_ReferralCode' AND object_id = OBJECT_ID('AspNetUsers'))
    CREATE INDEX IX_AspNetUsers_ReferralCode ON AspNetUsers (ReferralCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_ReferredByUserId' AND object_id = OBJECT_ID('AspNetUsers'))
    CREATE INDEX IX_AspNetUsers_ReferredByUserId ON AspNetUsers (ReferredByUserId);

-- 5-5 باب التقديم على البرامج الرائدة
IF COL_LENGTH('FlagshipPrograms', 'ApplicationsOpen') IS NULL
    ALTER TABLE FlagshipPrograms ADD ApplicationsOpen bit NOT NULL CONSTRAINT DF_FlagshipPrograms_ApplicationsOpen DEFAULT 0;

IF OBJECT_ID('ProgramInterests', 'U') IS NULL
    CREATE TABLE ProgramInterests (
        Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProgramInterests PRIMARY KEY,
        UserId     nvarchar(450) NOT NULL,
        ProgramId  int NOT NULL,
        CreatedAt  datetime2 NOT NULL,
        NotifiedAt datetime2 NULL,
        CONSTRAINT FK_ProgramInterests_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_ProgramInterests_FlagshipPrograms_ProgramId FOREIGN KEY (ProgramId) REFERENCES FlagshipPrograms (Id) ON DELETE CASCADE
    );
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProgramInterests_ProgramId' AND object_id = OBJECT_ID('ProgramInterests'))
    CREATE INDEX IX_ProgramInterests_ProgramId ON ProgramInterests (ProgramId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProgramInterests_UserId_ProgramId' AND object_id = OBJECT_ID('ProgramInterests'))
    CREATE UNIQUE INDEX IX_ProgramInterests_UserId_ProgramId ON ProgramInterests (UserId, ProgramId);

-- 5-4 خزانة الأعضاء
IF OBJECT_ID('Resources', 'U') IS NULL
    CREATE TABLE Resources (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Resources PRIMARY KEY,
        TitleAr       nvarchar(max) NOT NULL,
        TitleEn       nvarchar(max) NOT NULL,
        TitleFr       nvarchar(max) NOT NULL,
        DescriptionAr nvarchar(max) NULL,
        DescriptionEn nvarchar(max) NULL,
        DescriptionFr nvarchar(max) NULL,
        Category      int NOT NULL,
        FilePath      nvarchar(max) NULL,
        ExternalUrl   nvarchar(500) NULL,
        SortOrder     int NOT NULL,
        IsPublished   bit NOT NULL,
        CreatedAt     datetime2 NOT NULL
    );

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029142324_Community')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029142324_Community', '8.0.8');

-- للتحقق
SELECT COL_LENGTH('AspNetUsers', 'ReferralCode') AS has_referral, COL_LENGTH('FlagshipPrograms', 'ApplicationsOpen') AS has_apps_open;
SELECT name AS index_name FROM sys.indexes WHERE object_id IN (OBJECT_ID('ProgramInterests'), OBJECT_ID('AspNetUsers')) AND name LIKE 'IX_%Referr%' OR name LIKE 'IX_ProgramInterests%';
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;

