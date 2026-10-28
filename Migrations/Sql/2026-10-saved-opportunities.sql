-- الفرص المحفوظة (زر الإشارة المرجعية للمستخدم). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار: كل خطوة تتحقق أولًا، فتشغيله مرتين لا يغيّر شيئًا. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- بديل عن: dotnet ef database update (الهجرة 20261028174553_SavedOpportunities)
IF OBJECT_ID('SavedOpportunities', 'U') IS NULL
BEGIN
    CREATE TABLE SavedOpportunities (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SavedOpportunities PRIMARY KEY,
        UserId        nvarchar(450) NOT NULL,
        OpportunityId int NOT NULL,
        SavedAt       datetime2 NOT NULL,
        -- بلا cascade من جهة المستخدم (SQL Server يرفض مسارين متسلسلين)؛ الحذف من جهة الفرصة متسلسل
        CONSTRAINT FK_SavedOpportunities_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id),
        CONSTRAINT FK_SavedOpportunities_Opportunities_OpportunityId FOREIGN KEY (OpportunityId) REFERENCES Opportunities (Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SavedOpportunities_OpportunityId' AND object_id = OBJECT_ID('SavedOpportunities'))
    EXEC('CREATE INDEX IX_SavedOpportunities_OpportunityId ON SavedOpportunities (OpportunityId)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SavedOpportunities_UserId_OpportunityId' AND object_id = OBJECT_ID('SavedOpportunities'))
    EXEC('CREATE UNIQUE INDEX IX_SavedOpportunities_UserId_OpportunityId ON SavedOpportunities (UserId, OpportunityId)');

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261028174553_SavedOpportunities')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261028174553_SavedOpportunities'', ''8.0.8'')');

-- للتحقق: يجب أن يظهر الجدول بفهرسين وسطر الهجرة
SELECT name AS index_name FROM sys.indexes WHERE object_id = OBJECT_ID('SavedOpportunities') AND name LIKE 'IX_%';
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
