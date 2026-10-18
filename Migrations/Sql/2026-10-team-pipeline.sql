-- ربط الموقع بمنصة الفريق (مسار الفرص). ينفَّذ مرة واحدة على قاعدة الإنتاج
-- بديل عن: dotnet ef migrations add TeamPipeline && dotnet ef database update
IF COL_LENGTH('Opportunities', 'Status') IS NULL
    ALTER TABLE Opportunities ADD Status int NOT NULL CONSTRAINT DF_Opportunities_Status DEFAULT 1; -- 1 = Published (كل القديم منشور)
IF COL_LENGTH('Opportunities', 'SourceUrl') IS NULL
    ALTER TABLE Opportunities ADD SourceUrl nvarchar(max) NULL;
IF COL_LENGTH('Opportunities', 'PipeId') IS NULL
    ALTER TABLE Opportunities ADD PipeId int NULL;
IF COL_LENGTH('Opportunities', 'AiGenerated') IS NULL
    ALTER TABLE Opportunities ADD AiGenerated bit NOT NULL CONSTRAINT DF_Opportunities_AiGenerated DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Opportunities_PipeId')
    CREATE INDEX IX_Opportunities_PipeId ON Opportunities (PipeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Opportunities_Status')
    CREATE INDEX IX_Opportunities_Status ON Opportunities (Status);
-- إن طُبّق هذا السكربت يدويًا، سجّل الهجرة حتى لا يعيدها EF:
-- INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261018000000_TeamPipeline', '8.0.8');
