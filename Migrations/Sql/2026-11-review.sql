-- مراجعة فرص المنظمات قبل النشر + توثيق/إيقاف المنظمات (المرحلة 0.2 من خطة 2026).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261103233245_Review)
-- الحالات الجديدة في Opportunities.Status (int موجود): 2 = بانتظار المراجعة، 3 = مرفوضة، 4 = موقوفة. لا تغيير على الصفوف الحالية.

IF COL_LENGTH('Opportunities', 'ReviewNote') IS NULL
    ALTER TABLE Opportunities ADD ReviewNote nvarchar(max) NULL;

IF COL_LENGTH('Opportunities', 'ReviewedAt') IS NULL
    ALTER TABLE Opportunities ADD ReviewedAt datetime2 NULL;

IF COL_LENGTH('Organizations', 'IsVerified') IS NULL
    ALTER TABLE Organizations ADD IsVerified bit NOT NULL CONSTRAINT DF_Organizations_IsVerified DEFAULT 0;

IF COL_LENGTH('Organizations', 'IsSuspended') IS NULL
    ALTER TABLE Organizations ADD IsSuspended bit NOT NULL CONSTRAINT DF_Organizations_IsSuspended DEFAULT 0;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261103233245_Review')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261103233245_Review', '10.0.14');

-- للتحقق
SELECT COL_LENGTH('Opportunities', 'ReviewNote') AS has_review_note, COL_LENGTH('Organizations', 'IsVerified') AS has_is_verified, COL_LENGTH('Organizations', 'IsSuspended') AS has_is_suspended;
SELECT Status, COUNT(*) AS n FROM Opportunities GROUP BY Status;
