-- ربط الموقع بمنصة الفريق (مسار الفرص). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار: كل خطوة تتحقق أولًا، فتشغيله مرتين لا يغيّر شيئًا. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- بديل عن: dotnet ef database update
-- الفهارس وسجل الهجرة عبر EXEC حتى تُترجم بعد إضافة الأعمدة (يعمل في SSMS ولوحة site4now دون الحاجة إلى GO).
IF COL_LENGTH('Opportunities', 'Status') IS NULL
    ALTER TABLE Opportunities ADD Status int NOT NULL CONSTRAINT DF_Opportunities_Status DEFAULT 1; -- 1 = Published (كل القديم منشور)
IF COL_LENGTH('Opportunities', 'SourceUrl') IS NULL
    ALTER TABLE Opportunities ADD SourceUrl nvarchar(max) NULL;
IF COL_LENGTH('Opportunities', 'PipeId') IS NULL
    ALTER TABLE Opportunities ADD PipeId int NULL;
IF COL_LENGTH('Opportunities', 'AiGenerated') IS NULL
    ALTER TABLE Opportunities ADD AiGenerated bit NOT NULL CONSTRAINT DF_Opportunities_AiGenerated DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Opportunities_PipeId' AND object_id = OBJECT_ID('Opportunities'))
    EXEC('CREATE INDEX IX_Opportunities_PipeId ON Opportunities (PipeId)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Opportunities_Status' AND object_id = OBJECT_ID('Opportunities'))
    EXEC('CREATE INDEX IX_Opportunities_Status ON Opportunities (Status)');

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261018000000_TeamPipeline')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261018000000_TeamPipeline'', ''8.0.8'')');

-- للتحقق: يجب أن يظهر 4 أعمدة وسطر الهجرة
SELECT name AS added_column FROM sys.columns
WHERE object_id = OBJECT_ID('Opportunities') AND name IN ('Status', 'SourceUrl', 'PipeId', 'AiGenerated');
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
