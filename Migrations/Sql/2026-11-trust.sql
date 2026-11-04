-- المرحلة 3 (الثقة وتقرير المنظمات) — الجزء 1: البنية. ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261104205520_Stage3Trust)
-- بعده شغّل 2026-11-trust-data.sql في تنفيذ منفصل (UPDATE على عمود أُضيف في الدفعة نفسها يفشل عند الترجمة).

-- درجة التوثيق المعروضة للزوار (0 غير موثَّقة، 1 مسجَّلة، 2 موثَّقة، 3 شريك)
IF COL_LENGTH('Organizations', 'VerificationTier') IS NULL
    ALTER TABLE Organizations ADD VerificationTier int NOT NULL CONSTRAINT DF_Organizations_VerificationTier DEFAULT 0;

-- بلاغات الزوار على الفرص («أبلغ عن مشكلة»)
IF OBJECT_ID('OpportunityReports', 'U') IS NULL
BEGIN
    CREATE TABLE OpportunityReports (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpportunityReports PRIMARY KEY,
        OpportunityId int NOT NULL,
        Reason int NOT NULL,
        Details nvarchar(500) NULL,
        UserId nvarchar(450) NULL,
        Email nvarchar(256) NULL,
        ReporterKey nvarchar(64) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        Status int NOT NULL,
        ResolvedAt datetime2 NULL,
        ResolvedByUserId nvarchar(450) NULL,
        CONSTRAINT FK_OpportunityReports_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_OpportunityReports_OpportunityId_Status ON OpportunityReports (OpportunityId, Status);
END;

-- «تابع هذه المنظمة»
IF OBJECT_ID('OrganizationFollows', 'U') IS NULL
BEGIN
    CREATE TABLE OrganizationFollows (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrganizationFollows PRIMARY KEY,
        UserId nvarchar(450) NOT NULL,
        OrganizationId int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT FK_OrganizationFollows_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_OrganizationFollows_Organizations_OrganizationId FOREIGN KEY (OrganizationId) REFERENCES Organizations (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_OrganizationFollows_OrganizationId ON OrganizationFollows (OrganizationId);
    CREATE UNIQUE INDEX IX_OrganizationFollows_UserId_OrganizationId ON OrganizationFollows (UserId, OrganizationId);
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104205520_Stage3Trust')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261104205520_Stage3Trust', '10.0.14');

-- للتحقق
SELECT COL_LENGTH('Organizations', 'VerificationTier') AS has_tier,
       OBJECT_ID('OpportunityReports', 'U') AS has_reports,
       OBJECT_ID('OrganizationFollows', 'U') AS has_follows;
