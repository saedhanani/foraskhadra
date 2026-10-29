-- المرحلة 4 من خطة العودة: "جديد منذ آخر زيارة" (عمودان على المستخدم). ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- (شارات الإنجاز وعدّاد الحفظ واكتمال الملف والعدّاد التنازلي تُحسب من بيانات موجودة ولا تحتاج تغييرًا في القاعدة.)
-- آمن للتكرار. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029140651_LastSeen)

IF COL_LENGTH('AspNetUsers', 'LastSeenAt') IS NULL
    ALTER TABLE AspNetUsers ADD LastSeenAt datetime2 NULL;

IF COL_LENGTH('AspNetUsers', 'PreviousVisitAt') IS NULL
    ALTER TABLE AspNetUsers ADD PreviousVisitAt datetime2 NULL;

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029140651_LastSeen')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261029140651_LastSeen', '8.0.8');

-- للتحقق
SELECT COL_LENGTH('AspNetUsers', 'LastSeenAt') AS has_last_seen, COL_LENGTH('AspNetUsers', 'PreviousVisitAt') AS has_previous_visit;
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
