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

-- للتحقق
SELECT name AS index_name FROM sys.indexes WHERE object_id = OBJECT_ID('CycleWatches') AND name LIKE 'IX_%';
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
