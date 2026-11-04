-- المرحلة 3 — الجزء 2: بيانات. ينفَّذ بعد 2026-11-trust.sql في تنفيذ منفصل. آمن للتكرار.
-- المنظمات الموثَّقة أصلًا (IsVerified = 1) تأخذ درجة «موثَّقة» (2) حتى تظهر شارتها؛ بقية المنظمات بحساب «مسجَّلة» (1).

UPDATE Organizations SET VerificationTier = 2 WHERE IsVerified = 1 AND VerificationTier < 2;
UPDATE Organizations SET VerificationTier = 1 WHERE IsVerified = 0 AND VerificationTier = 0 AND UserId IS NOT NULL;

-- للتحقق
SELECT VerificationTier, COUNT(*) AS organizations FROM Organizations GROUP BY VerificationTier;
