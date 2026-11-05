-- المرحلة 4 (النمو والمجتمع والإيرادات): البنية كاملة في تنفيذ واحد. ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. خذ نسخة احتياطية قبل التشغيل.
-- بديل عن: dotnet ef database update (الهجرة 20261105083603_Stage4Growth)
-- المحتوى (8 أدلة تقديم) يُستورد بعد النشر من /Admin/Guides → «استيراد الدفعة الأولى»، أو بسكربت SQL مولَّد عند الحاجة.

-- فرصة مموَّلة (4.5)
IF COL_LENGTH('Opportunities', 'IsSponsored') IS NULL
    ALTER TABLE Opportunities ADD IsSponsored bit NOT NULL CONSTRAINT DF_Opportunities_IsSponsored DEFAULT 0;
IF COL_LENGTH('Opportunities', 'SponsoredUntil') IS NULL
    ALTER TABLE Opportunities ADD SponsoredUntil datetime2 NULL;

-- مهام السفراء (4.4)
IF OBJECT_ID('AmbassadorTaskSubmissions', 'U') IS NULL
BEGIN
    CREATE TABLE AmbassadorTaskSubmissions (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AmbassadorTaskSubmissions PRIMARY KEY,
        UserId nvarchar(450) NOT NULL,
        TaskKey nvarchar(40) NOT NULL,
        Month nvarchar(7) NOT NULL,
        Note nvarchar(500) NULL,
        ProofUrl nvarchar(500) NULL,
        Status int NOT NULL,
        PointsAwarded int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        ReviewedAt datetime2 NULL,
        ReviewedByUserId nvarchar(450) NULL,
        CONSTRAINT FK_AmbassadorTaskSubmissions_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id)
    );
    CREATE UNIQUE INDEX IX_AmbassadorTaskSubmissions_UserId_TaskKey_Month ON AmbassadorTaskSubmissions (UserId, TaskKey, Month);
END;

-- أدلة التقديم (4.1)
IF OBJECT_ID('ApplicationGuides', 'U') IS NULL
BEGIN
    CREATE TABLE ApplicationGuides (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApplicationGuides PRIMARY KEY,
        ProgramKey nvarchar(60) NOT NULL,
        Slug nvarchar(80) NOT NULL,
        TitleAr nvarchar(200) NOT NULL,
        TitleEn nvarchar(200) NOT NULL,
        TitleFr nvarchar(200) NOT NULL,
        SummaryAr nvarchar(400) NULL,
        SummaryEn nvarchar(400) NULL,
        SummaryFr nvarchar(400) NULL,
        SectionsJson nvarchar(max) NOT NULL,
        OfficialUrl nvarchar(500) NULL,
        OpensMonth int NULL,
        ClosesMonth int NULL,
        IsPublished bit NOT NULL,
        SortOrder int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX IX_ApplicationGuides_ProgramKey ON ApplicationGuides (ProgramKey);
    CREATE UNIQUE INDEX IX_ApplicationGuides_Slug ON ApplicationGuides (Slug);
END;

-- التعزيزات (4.5)
IF OBJECT_ID('OpportunityBoosts', 'U') IS NULL
BEGIN
    CREATE TABLE OpportunityBoosts (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpportunityBoosts PRIMARY KEY,
        OpportunityId int NOT NULL,
        Recipients int NOT NULL,
        CreatedByUserId nvarchar(450) NULL,
        Note nvarchar(200) NULL,
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT FK_OpportunityBoosts_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_OpportunityBoosts_OpportunityId ON OpportunityBoosts (OpportunityId);
END;

-- أسئلة وأجوبة على الفرص (4.6)
IF OBJECT_ID('OpportunityQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE OpportunityQuestions (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpportunityQuestions PRIMARY KEY,
        OpportunityId int NOT NULL,
        QuestionAr nvarchar(300) NOT NULL,
        QuestionEn nvarchar(300) NULL,
        QuestionFr nvarchar(300) NULL,
        AnswerAr nvarchar(1500) NULL,
        AnswerEn nvarchar(1500) NULL,
        AnswerFr nvarchar(1500) NULL,
        Lang nvarchar(2) NOT NULL,
        UserId nvarchar(450) NULL,
        IsCurated bit NOT NULL,
        Status int NOT NULL,
        SortOrder int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        AnsweredAt datetime2 NULL,
        AnsweredByUserId nvarchar(450) NULL,
        CONSTRAINT FK_OpportunityQuestions_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_OpportunityQuestions_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_OpportunityQuestions_OpportunityId_Status ON OpportunityQuestions (OpportunityId, Status);
    CREATE INDEX IX_OpportunityQuestions_UserId ON OpportunityQuestions (UserId);
END;

-- قصص النجاح (4.2)
IF OBJECT_ID('SuccessStories', 'U') IS NULL
BEGIN
    CREATE TABLE SuccessStories (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SuccessStories PRIMARY KEY,
        UserId nvarchar(450) NOT NULL,
        OpportunityId int NULL,
        ProgramName nvarchar(200) NULL,
        Story nvarchar(1500) NOT NULL,
        Advice nvarchar(300) NULL,
        DisplayName int NOT NULL,
        ShownName nvarchar(120) NULL,
        CountryIso nvarchar(2) NULL,
        ConsentPublic bit NOT NULL,
        Lang nvarchar(2) NOT NULL,
        Status int NOT NULL,
        CreatedAt datetime2 NOT NULL,
        ReviewedAt datetime2 NULL,
        ReviewedByUserId nvarchar(450) NULL,
        Featured bit NOT NULL,
        CONSTRAINT FK_SuccessStories_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_SuccessStories_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_SuccessStories_OpportunityId ON SuccessStories (OpportunityId);
    CREATE INDEX IX_SuccessStories_Status_Featured ON SuccessStories (Status, Featured);
    CREATE INDEX IX_SuccessStories_UserId ON SuccessStories (UserId);
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261105083603_Stage4Growth')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261105083603_Stage4Growth', '10.0.14');

-- للتحقق
SELECT COL_LENGTH('Opportunities', 'IsSponsored') AS has_sponsored,
       OBJECT_ID('ApplicationGuides', 'U') AS has_guides,
       OBJECT_ID('SuccessStories', 'U') AS has_stories,
       OBJECT_ID('OpportunityQuestions', 'U') AS has_questions,
       OBJECT_ID('AmbassadorTaskSubmissions', 'U') AS has_tasks,
       OBJECT_ID('OpportunityBoosts', 'U') AS has_boosts;
