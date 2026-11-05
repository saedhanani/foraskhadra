-- المرحلة 5 (القياس والجودة): عمود وقت دخول الفرصة طابور المراجعة — زمن المراجعة في لوحة المؤشرات /Admin/Analytics.
-- تنفيذ واحد، آمن للتكرار، بلا تغيير في البيانات. ينفَّذ على قاعدة الإنتاج قبل نشر كود المرحلة 5.
-- بديل عن: dotnet ef database update (الهجرة 20261105105747_Stage5Kpi)

IF COL_LENGTH('Opportunities', 'SubmittedAt') IS NULL
    ALTER TABLE Opportunities ADD SubmittedAt datetime2 NULL;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261105105747_Stage5Kpi')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261105105747_Stage5Kpi', '10.0.14');

-- للتحقق
SELECT COL_LENGTH('Opportunities', 'SubmittedAt') AS has_submitted_at,
       (SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId = '20261105105747_Stage5Kpi') AS migration_recorded;
