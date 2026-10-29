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

-- للتحقق
SELECT COL_LENGTH('SavedOpportunities', 'Status') AS has_status_col, COL_LENGTH('SavedOpportunities', 'StatusChangedAt') AS has_status_changed_col;
SELECT name AS index_name FROM sys.indexes WHERE object_id IN (OBJECT_ID('ChecklistItems'), OBJECT_ID('RecentViews')) AND name LIKE 'IX_%';
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
