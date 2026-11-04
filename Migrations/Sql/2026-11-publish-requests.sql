-- بوابة المنظمات (المرحلة 1.2 من خطة 2026): جدول طلبات النشر بلا حساب (/for-organizations → PublishRequests).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261104133908_PublishRequests)
-- الحالات في Status: 0 = جديد، 1 = قيد الكتابة، 2 = نُشر، 3 = مرفوض، 4 = مزعج.
-- نوع الإشعار الجديد في Notifications.Kind (int موجود): 9 = طلب نشر جديد (للمديرين). لا تغيير على الصفوف الحالية.

IF OBJECT_ID('PublishRequests', 'U') IS NULL
BEGIN
    CREATE TABLE PublishRequests (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PublishRequests PRIMARY KEY,
        OrganizationName nvarchar(200) NOT NULL,
        ContactName nvarchar(120) NULL,
        ContactEmail nvarchar(256) NOT NULL,
        Website nvarchar(500) NULL,
        SourceUrl nvarchar(1000) NOT NULL,
        Notes nvarchar(2000) NULL,
        TypeHint int NULL,
        CountryIso nvarchar(2) NULL,
        Language nvarchar(2) NOT NULL,
        EstimatedReach int NOT NULL,
        Status int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        TakenAt datetime2 NULL,
        ClosedAt datetime2 NULL,
        OpportunityId int NULL,
        AdminNote nvarchar(500) NULL,
        CONSTRAINT FK_PublishRequests_Opportunities_OpportunityId FOREIGN KEY (OpportunityId)
            REFERENCES Opportunities (Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_PublishRequests_OpportunityId ON PublishRequests (OpportunityId);
    CREATE INDEX IX_PublishRequests_Status ON PublishRequests (Status);
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104133908_PublishRequests')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261104133908_PublishRequests', '10.0.14');

-- للتحقق
SELECT OBJECT_ID('PublishRequests', 'U') AS has_publish_requests;
SELECT COUNT(*) AS n FROM PublishRequests;
