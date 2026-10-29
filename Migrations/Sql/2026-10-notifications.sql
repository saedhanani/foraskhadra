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

-- للتحقق: يجب أن يظهر الجدولان بفهارسهما وعمود المستخدم الجديد وسطر الهجرة
SELECT name AS index_name FROM sys.indexes WHERE object_id = OBJECT_ID('Notifications') AND name LIKE 'IX_%';
SELECT name AS index_name FROM sys.indexes WHERE object_id = OBJECT_ID('SavedSearches') AND name LIKE 'IX_%';
SELECT COL_LENGTH('AspNetUsers', 'LastInterestNotifiedAt') AS has_last_interest_col;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
