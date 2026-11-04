-- تأكيد البريد (المرحلة 0.4 من خطة 2026): الحسابات المسجَّلة قبل هذا التحديث تُعلَّم مؤكَّدة حتى لا يُغلق رابط التقديم في وجه أحد.
-- الحسابات الجديدة تستلم رابط تأكيد عند التسجيل، ورابط «قدّم الآن» (/go/) يتطلب التأكيد.
-- ينفَّذ على قاعدة الإنتاج مع نشر الكود. آمن للتكرار. بلا EXEC. لا هجرة EF (بيانات فقط).
UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;
SELECT COUNT(*) AS users_total, SUM(CASE WHEN EmailConfirmed = 1 THEN 1 ELSE 0 END) AS confirmed FROM AspNetUsers;
