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
