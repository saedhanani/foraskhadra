-- «بوصلة» (المرحلة 1.1 من خطة 2026): ملف أهلية العضو (1:1 مع AspNetUsers).
-- ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد. آمن للتكرار. خذ نسخة احتياطية قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261104145146_EligibilityProfiles)
-- إجابات الزائر تبقى في كوكي fk_compass ولا تُكتب هنا إلا بعد التسجيل أو الدخول.

IF OBJECT_ID('EligibilityProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE EligibilityProfiles (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EligibilityProfiles PRIMARY KEY,
        UserId nvarchar(450) NOT NULL,
        ResidenceIso nvarchar(2) NULL,
        NationalityIso nvarchar(2) NULL,
        BirthYear int NULL,
        Gender nvarchar(1) NULL,
        Degree nvarchar(20) NULL,
        FieldKeys nvarchar(400) NULL,
        GraduationYear int NULL,
        University nvarchar(150) NULL,
        EnglishTests nvarchar(40) NULL,
        EnglishScore nvarchar(20) NULL,
        FrenchCertificate bit NOT NULL,
        ExperienceYears int NULL,
        Goals nvarchar(200) NULL,
        HostCountries nvarchar(200) NULL,
        FullyFundedOnly bit NOT NULL,
        RemoteOnly bit NOT NULL,
        StartWithinMonths int NULL,
        Documents nvarchar(400) NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        CONSTRAINT FK_EligibilityProfiles_AspNetUsers_UserId FOREIGN KEY (UserId)
            REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_EligibilityProfiles_UserId ON EligibilityProfiles (UserId);
END;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261104145146_EligibilityProfiles')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261104145146_EligibilityProfiles', '10.0.14');

-- للتحقق
SELECT OBJECT_ID('EligibilityProfiles', 'U') AS has_eligibility_profiles;
