-- إشعارات الدفع (تطبيق الهاتف): جدول PushSubscriptions + عمود Notifications.PushedAt. ينفَّذ على قاعدة الإنتاج قبل نشر الكود الجديد.
-- آمن للتكرار. خذ نسخة احتياطية من القاعدة قبل التشغيل. بلا EXEC إطلاقًا.
-- بديل عن: dotnet ef database update (الهجرة 20261029190916_PushNotifications)

IF COL_LENGTH('Notifications', 'PushedAt') IS NULL
    ALTER TABLE Notifications ADD PushedAt datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_PushedAt' AND object_id = OBJECT_ID('Notifications'))
    CREATE INDEX IX_Notifications_PushedAt ON Notifications (PushedAt);

IF OBJECT_ID('PushSubscriptions') IS NULL
    CREATE TABLE PushSubscriptions (Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PushSubscriptions PRIMARY KEY, UserId nvarchar(450) NOT NULL CONSTRAINT FK_PushSubscriptions_AspNetUsers_UserId FOREIGN KEY REFERENCES AspNetUsers (Id) ON DELETE CASCADE, Endpoint nvarchar(1000) NOT NULL, P256dh nvarchar(200) NOT NULL, Auth nvarchar(100) NOT NULL, Lang nvarchar(5) NOT NULL, TzOffsetMinutes int NOT NULL, CreatedAt datetime2 NOT NULL, LastSentAt datetime2 NULL);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PushSubscriptions_UserId' AND object_id = OBJECT_ID('PushSubscriptions'))
    CREATE INDEX IX_PushSubscriptions_UserId ON PushSubscriptions (UserId);

-- تسجيل الهجرة حتى لا يحاول EF تطبيقها مرة أخرى
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261029190916_PushNotifications')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261029190916_PushNotifications', '8.0.8');

-- للتحقق
SELECT COL_LENGTH('Notifications', 'PushedAt') AS has_pushed_at, OBJECT_ID('PushSubscriptions') AS push_table_id;
SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
