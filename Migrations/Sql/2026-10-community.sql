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
