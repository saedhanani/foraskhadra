-- المرحلة 0 من خطة 2026 — كل سكربتات قاعدة البيانات بالترتيب، في ملف واحد لبوابة SQL Studio.
-- خذ نسخة احتياطية من اللوحة أولًا. آمن للتكرار. بلا EXEC وبلا كلمة ENDPOINT. ينفَّذ قبل رفع الكود.

-- ===================== 2026-11-review.sql =====================
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

-- ===================== 2026-11-apply-clicks.sql =====================
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

-- ===================== 2026-11-opportunity-fields.sql =====================
-- حقول الأهلية المهيكلة للفرصة + جدول المجالات + رموز ISO للدول (المرحلة 0.5 من خطة 2026؛ العقد مع منصة الكتابة في docs/PIPELINE-PROMPT-v2.md).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261103234939_OpportunityFacets)

-- 1) أعمدة الفرصة (كلها لها قيمة افتراضية أو NULL؛ الصفوف الحالية لا تتغير)
IF COL_LENGTH('Opportunities', 'FundingType') IS NULL ALTER TABLE Opportunities ADD FundingType int NOT NULL CONSTRAINT DF_Opp_FundingType DEFAULT 0;
IF COL_LENGTH('Opportunities', 'DegreeLevels') IS NULL ALTER TABLE Opportunities ADD DegreeLevels int NOT NULL CONSTRAINT DF_Opp_DegreeLevels DEFAULT 0;
IF COL_LENGTH('Opportunities', 'GreenThemes') IS NULL ALTER TABLE Opportunities ADD GreenThemes int NOT NULL CONSTRAINT DF_Opp_GreenThemes DEFAULT 0;
IF COL_LENGTH('Opportunities', 'EligibleCountriesIso') IS NULL ALTER TABLE Opportunities ADD EligibleCountriesIso nvarchar(400) NULL;
IF COL_LENGTH('Opportunities', 'HostCountryIso') IS NULL ALTER TABLE Opportunities ADD HostCountryIso nvarchar(2) NULL;
IF COL_LENGTH('Opportunities', 'IsRemote') IS NULL ALTER TABLE Opportunities ADD IsRemote bit NOT NULL CONSTRAINT DF_Opp_IsRemote DEFAULT 0;
IF COL_LENGTH('Opportunities', 'IsOnline') IS NULL ALTER TABLE Opportunities ADD IsOnline bit NOT NULL CONSTRAINT DF_Opp_IsOnline DEFAULT 0;
IF COL_LENGTH('Opportunities', 'MinAge') IS NULL ALTER TABLE Opportunities ADD MinAge int NULL;
IF COL_LENGTH('Opportunities', 'MaxAge') IS NULL ALTER TABLE Opportunities ADD MaxAge int NULL;
IF COL_LENGTH('Opportunities', 'LanguageRequirement') IS NULL ALTER TABLE Opportunities ADD LanguageRequirement int NOT NULL CONSTRAINT DF_Opp_LanguageRequirement DEFAULT 0;
IF COL_LENGTH('Opportunities', 'LanguageScore') IS NULL ALTER TABLE Opportunities ADD LanguageScore nvarchar(100) NULL;
IF COL_LENGTH('Opportunities', 'HasApplicationFee') IS NULL ALTER TABLE Opportunities ADD HasApplicationFee bit NOT NULL CONSTRAINT DF_Opp_HasApplicationFee DEFAULT 0;
IF COL_LENGTH('Opportunities', 'ForWomenOnly') IS NULL ALTER TABLE Opportunities ADD ForWomenOnly bit NOT NULL CONSTRAINT DF_Opp_ForWomenOnly DEFAULT 0;
IF COL_LENGTH('Opportunities', 'StartDate') IS NULL ALTER TABLE Opportunities ADD StartDate datetime2 NULL;
IF COL_LENGTH('Opportunities', 'Venue') IS NULL ALTER TABLE Opportunities ADD Venue nvarchar(200) NULL;
IF COL_LENGTH('Opportunities', 'HostOrganizationName') IS NULL ALTER TABLE Opportunities ADD HostOrganizationName nvarchar(200) NULL;
IF COL_LENGTH('Opportunities', 'HostOrganizationUrl') IS NULL ALTER TABLE Opportunities ADD HostOrganizationUrl nvarchar(max) NULL;
IF COL_LENGTH('Opportunities', 'HostOrganizationLogoUrl') IS NULL ALTER TABLE Opportunities ADD HostOrganizationLogoUrl nvarchar(max) NULL;
IF COL_LENGTH('Opportunities', 'EmploymentType') IS NULL ALTER TABLE Opportunities ADD EmploymentType int NOT NULL CONSTRAINT DF_Opp_EmploymentType DEFAULT 0;
IF COL_LENGTH('Opportunities', 'ProgramKey') IS NULL ALTER TABLE Opportunities ADD ProgramKey nvarchar(60) NULL;
IF COL_LENGTH('Opportunities', 'BenefitsSummaryJson') IS NULL ALTER TABLE Opportunities ADD BenefitsSummaryJson nvarchar(max) NULL;
IF COL_LENGTH('Opportunities', 'LastCheckedAt') IS NULL ALTER TABLE Opportunities ADD LastCheckedAt datetime2 NULL;
IF COL_LENGTH('Opportunities', 'UpdatedAt') IS NULL ALTER TABLE Opportunities ADD UpdatedAt datetime2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Opportunities_ProgramKey' AND object_id = OBJECT_ID('Opportunities'))
    CREATE INDEX IX_Opportunities_ProgramKey ON Opportunities (ProgramKey);

-- 2) رموز ISO للدول (XA = الوطن العربي، XW = كل العالم)
IF COL_LENGTH('Countries', 'IsoCode') IS NULL ALTER TABLE Countries ADD IsoCode nvarchar(2) NULL;
UPDATE Countries SET IsoCode = 'XW' WHERE Id = 1;
UPDATE Countries SET IsoCode = 'XA' WHERE Id = 2;
UPDATE Countries SET IsoCode = 'PS' WHERE Id = 3;
UPDATE Countries SET IsoCode = 'JO' WHERE Id = 4;
UPDATE Countries SET IsoCode = 'SY' WHERE Id = 5;
UPDATE Countries SET IsoCode = 'LB' WHERE Id = 6;
UPDATE Countries SET IsoCode = 'IQ' WHERE Id = 7;
UPDATE Countries SET IsoCode = 'SA' WHERE Id = 8;
UPDATE Countries SET IsoCode = 'KW' WHERE Id = 9;
UPDATE Countries SET IsoCode = 'QA' WHERE Id = 10;
UPDATE Countries SET IsoCode = 'BH' WHERE Id = 11;
UPDATE Countries SET IsoCode = 'AE' WHERE Id = 12;
UPDATE Countries SET IsoCode = 'OM' WHERE Id = 13;
UPDATE Countries SET IsoCode = 'YE' WHERE Id = 14;
UPDATE Countries SET IsoCode = 'EG' WHERE Id = 15;
UPDATE Countries SET IsoCode = 'SD' WHERE Id = 16;
UPDATE Countries SET IsoCode = 'LY' WHERE Id = 17;
UPDATE Countries SET IsoCode = 'TN' WHERE Id = 18;
UPDATE Countries SET IsoCode = 'DZ' WHERE Id = 19;
UPDATE Countries SET IsoCode = 'MA' WHERE Id = 20;
UPDATE Countries SET IsoCode = 'MR' WHERE Id = 21;
UPDATE Countries SET IsoCode = 'DJ' WHERE Id = 22;
UPDATE Countries SET IsoCode = 'SO' WHERE Id = 23;
UPDATE Countries SET IsoCode = 'KM' WHERE Id = 24;

-- 3) المجالات وجدول الربط
IF OBJECT_ID('Fields', 'U') IS NULL
    CREATE TABLE Fields (
        Id int NOT NULL CONSTRAINT PK_Fields PRIMARY KEY,
        [Key] nvarchar(40) NOT NULL,
        NameAr nvarchar(max) NOT NULL,
        NameEn nvarchar(max) NOT NULL,
        NameFr nvarchar(max) NOT NULL,
        SortOrder int NOT NULL
    );
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fields_Key' AND object_id = OBJECT_ID('Fields'))
    CREATE UNIQUE INDEX IX_Fields_Key ON Fields ([Key]);
IF OBJECT_ID('OpportunityFields', 'U') IS NULL
    CREATE TABLE OpportunityFields (
        FieldsId int NOT NULL,
        OpportunitiesId int NOT NULL,
        CONSTRAINT PK_OpportunityFields PRIMARY KEY (FieldsId, OpportunitiesId),
        CONSTRAINT FK_OpportunityFields_Fields_FieldsId FOREIGN KEY (FieldsId) REFERENCES Fields (Id) ON DELETE CASCADE,
        CONSTRAINT FK_OpportunityFields_Opportunities_OpportunitiesId FOREIGN KEY (OpportunitiesId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OpportunityFields_OpportunitiesId' AND object_id = OBJECT_ID('OpportunityFields'))
    CREATE INDEX IX_OpportunityFields_OpportunitiesId ON OpportunityFields (OpportunitiesId);

-- المجالات الـ25 (المفاتيح هي العقد مع منصة الكتابة؛ لا تُعاد تسميتها)
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 1) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (1, 'environment', N'البيئة والمناخ', N'Environment & Climate', N'Environnement et climat', 1);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 2) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (2, 'energy', N'الطاقة', N'Energy', N'Énergie', 2);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 3) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (3, 'water', N'المياه', N'Water', N'Eau', 3);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 4) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (4, 'agriculture', N'الزراعة والغذاء', N'Agriculture & Food', N'Agriculture et alimentation', 4);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 5) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (5, 'engineering', N'الهندسة', N'Engineering', N'Ingénierie', 5);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 6) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (6, 'architecture', N'العمارة والتخطيط العمراني', N'Architecture & Urban Planning', N'Architecture et urbanisme', 6);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 7) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (7, 'it', N'تقنية المعلومات والبرمجة', N'Computing & IT', N'Informatique', 7);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 8) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (8, 'data-ai', N'البيانات والذكاء الاصطناعي', N'Data & AI', N'Données et IA', 8);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 9) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (9, 'natural-sciences', N'العلوم الطبيعية', N'Natural Sciences', N'Sciences naturelles', 9);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 10) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (10, 'health', N'الصحة والطب', N'Health & Medicine', N'Santé et médecine', 10);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 11) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (11, 'business', N'الأعمال والإدارة', N'Business & Management', N'Commerce et gestion', 11);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 12) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (12, 'economics', N'الاقتصاد والتمويل', N'Economics & Finance', N'Économie et finance', 12);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 13) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (13, 'entrepreneurship', N'ريادة الأعمال والابتكار', N'Entrepreneurship & Innovation', N'Entrepreneuriat et innovation', 13);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 14) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (14, 'law', N'القانون', N'Law', N'Droit', 14);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 15) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (15, 'political-science', N'العلوم السياسية والعلاقات الدولية', N'Political Science & International Relations', N'Sciences politiques et relations internationales', 15);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 16) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (16, 'development', N'التنمية والعمل الإنساني', N'Development & Humanitarian Work', N'Développement et action humanitaire', 16);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 17) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (17, 'leadership', N'القيادة والمجتمع المدني', N'Leadership & Civil Society', N'Leadership et société civile', 17);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 18) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (18, 'education', N'التعليم', N'Education', N'Éducation', 18);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 19) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (19, 'social-sciences', N'العلوم الاجتماعية', N'Social Sciences', N'Sciences sociales', 19);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 20) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (20, 'languages', N'اللغات والأدب', N'Languages & Literature', N'Langues et littérature', 20);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 21) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (21, 'arts', N'الفنون والتصميم', N'Arts & Design', N'Arts et design', 21);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 22) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (22, 'media', N'الإعلام والاتصال', N'Media & Communication', N'Médias et communication', 22);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 23) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (23, 'tourism', N'السياحة والضيافة', N'Tourism & Hospitality', N'Tourisme et hôtellerie', 23);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 24) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (24, 'vocational', N'المهن والحرف', N'Vocational & Trades', N'Métiers et formation professionnelle', 24);
IF NOT EXISTS (SELECT 1 FROM Fields WHERE Id = 25) INSERT INTO Fields (Id, [Key], NameAr, NameEn, NameFr, SortOrder) VALUES (25, 'any', N'كل المجالات', N'All fields', N'Tous les domaines', 25);

-- 4) تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261103234939_OpportunityFacets')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261103234939_OpportunityFacets', '10.0.14');

-- للتحقق
SELECT COL_LENGTH('Opportunities', 'FundingType') AS has_funding, COL_LENGTH('Countries', 'IsoCode') AS has_iso, (SELECT COUNT(*) FROM Fields) AS fields_count, (SELECT COUNT(*) FROM Countries WHERE IsoCode IS NULL) AS countries_without_iso;

-- ===================== 2026-11-reels-request-fk.sql =====================
-- تنظيف: حذف عمود الظل OpportunityId1 من ReelsRequests (كان EF ينشئه لأن العلاقة لم تُربط بالتنقّل Opportunity.ReelsRequests). المرحلة 0.9 من خطة 2026.
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. بلا EXEC.
-- بديل عن: dotnet ef database update (الهجرة 20261104001801_ReelsRequestFk)
IF OBJECT_ID('FK_ReelsRequests_Opportunities_OpportunityId1', 'F') IS NOT NULL
    ALTER TABLE ReelsRequests DROP CONSTRAINT FK_ReelsRequests_Opportunities_OpportunityId1;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReelsRequests_OpportunityId1' AND object_id = OBJECT_ID('ReelsRequests'))
    DROP INDEX IX_ReelsRequests_OpportunityId1 ON ReelsRequests;
IF COL_LENGTH('ReelsRequests', 'OpportunityId1') IS NOT NULL
    ALTER TABLE ReelsRequests DROP COLUMN OpportunityId1;
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104001801_ReelsRequestFk')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261104001801_ReelsRequestFk', '10.0.14');
SELECT COL_LENGTH('ReelsRequests', 'OpportunityId1') AS shadow_column_should_be_null;

-- ===================== 2026-11-email-confirmed.sql =====================
-- تأكيد البريد (المرحلة 0.4 من خطة 2026): الحسابات المسجَّلة قبل هذا التحديث تُعلَّم مؤكَّدة حتى لا يُغلق رابط التقديم في وجه أحد.
-- الحسابات الجديدة تستلم رابط تأكيد عند التسجيل، ورابط «قدّم الآن» (/go/) يتطلب التأكيد.
-- ينفَّذ على قاعدة الإنتاج مع نشر الكود. آمن للتكرار. بلا EXEC. لا هجرة EF (بيانات فقط).
UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;
SELECT COUNT(*) AS users_total, SUM(CASE WHEN EmailConfirmed = 1 THEN 1 ELSE 0 END) AS confirmed FROM AspNetUsers;

-- ===================== 2026-11-arab-world-wording.sql =====================
-- توحيد المصطلح: "شباب الوطن العربي" بدل "الشباب العربي" في اسم برنامج بناء القدرات المخزَّن في قاعدة الإنتاج.
-- آمن للتكرار، بلا EXEC. لا هجرة EF (بيانات فقط). يمكن تنفيذه قبل نشر الكود أو بعده.
UPDATE FlagshipPrograms SET MenuTitleAr = N'برنامج فرص خضراء لبناء قدرات شباب الوطن العربي' WHERE Slug = 'youth-capacity-building' AND MenuTitleAr = N'برنامج فرص خضراء لبناء قدرات الشباب العربي';
UPDATE FlagshipPrograms SET TitleAr = N'برنامج فرص خضراء لبناء قدرات شباب الوطن العربي' WHERE Slug = 'youth-capacity-building' AND TitleAr = N'برنامج فرص خضراء لبناء قدرات الشباب العربي';
UPDATE FlagshipPrograms SET MenuTitleEn = N'Arab World Youth Capacity-Building Programme' WHERE Slug = 'youth-capacity-building' AND MenuTitleEn = N'Arab Youth Capacity-Building Programme';
UPDATE FlagshipPrograms SET TitleEn = N'Foras Khadra Capacity-Building Programme for Youth in the Arab World' WHERE Slug = 'youth-capacity-building' AND TitleEn = N'Foras Khadra Arab Youth Capacity-Building Programme';
UPDATE FlagshipPrograms SET TitleFr = N'Programme Foras Khadra de renforcement des capacités des jeunes du monde arabe' WHERE Slug = 'youth-capacity-building' AND TitleFr = N'Programme Foras Khadra de renforcement des capacités des jeunes arabes';
SELECT Slug, MenuTitleAr, TitleAr, TitleEn, TitleFr FROM FlagshipPrograms WHERE Slug = 'youth-capacity-building';

