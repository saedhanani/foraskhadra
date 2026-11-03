-- بوابة روابط التقديم /go/{id} + جدول الإحصاء اليومي لكل فرصة (المرحلة 0.3 من خطة 2026).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261103234112_ApplyClicks)

IF OBJECT_ID('OpportunityDailyStats', 'U') IS NULL
    CREATE TABLE OpportunityDailyStats (
        OpportunityId int NOT NULL,
        Day date NOT NULL,
        ApplyClicks int NOT NULL CONSTRAINT DF_OpportunityDailyStats_ApplyClicks DEFAULT 0,
        Views int NOT NULL CONSTRAINT DF_OpportunityDailyStats_Views DEFAULT 0,
        CONSTRAINT PK_OpportunityDailyStats PRIMARY KEY (OpportunityId, Day),
        CONSTRAINT FK_OpportunityDailyStats_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261103234112_ApplyClicks')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261103234112_ApplyClicks', '10.0.14');

-- للتحقق
SELECT OBJECT_ID('OpportunityDailyStats', 'U') AS stats_table;

-- روابط قديمة ليست http/https: الكود الجديد يرفض فتحها (404 في /go/) — راجعها وصحّحها من صفحة التعديل
SELECT Id, TitleAr, ApplyLink FROM Opportunities WHERE ApplyLink NOT LIKE 'http://%' AND ApplyLink NOT LIKE 'https://%';
SELECT Id, Name, Website FROM Organizations WHERE Website IS NOT NULL AND Website <> '' AND Website NOT LIKE 'http://%' AND Website NOT LIKE 'https://%';
