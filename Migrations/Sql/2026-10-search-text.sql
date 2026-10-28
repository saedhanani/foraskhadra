-- عمود نص البحث المطبّع للفرص. ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. التطبيق يملأ العمود للصفوف القديمة تلقائيًا عند أول تشغيل بعد النشر (Program.cs)،
-- فلا حاجة لملئه هنا. خذ نسخة احتياطية من القاعدة قبل التشغيل.
-- بديل عن: dotnet ef database update (الهجرة 20261028175428_OpportunitySearchText)
IF COL_LENGTH('Opportunities', 'SearchText') IS NULL
    ALTER TABLE Opportunities ADD SearchText nvarchar(max) NULL;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261028175428_OpportunitySearchText')
    EXEC('INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (''20261028175428_OpportunitySearchText'', ''8.0.8'')');

-- للتحقق
SELECT COL_LENGTH('Opportunities', 'SearchText') AS search_text_column;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
