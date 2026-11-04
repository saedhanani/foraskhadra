# نشر المرحلة 1 (الدفعة الأولى: 1.2 + 1.3 + 1.4 + 1.5) — قائمة التحقق

## ✅ نُشر على الإنتاج — 4 نوفمبر 2026 (مساءً)

الإصدار الحي الآن `d1e19bc` (المرحلة 1 كاملة: 1.1–1.6). ما جرى: نسخة احتياطية من اللوحة → السكربتات الثلاثة في SQL Studio دفعة واحدة (جداول جديدة فقط؛ `__EFMigrationsHistory` فيه الهجرات السبع من `20261103233245_Review` إلى `20261104145146_EligibilityProfiles`) → رفع `app_offline.htm` + `update-part1.zip` (6 ملفات، 2.6 MB: DLL وpdb وstaticwebassets وcompass.css) معًا → فك الضغط إلى `/site1/` (النجاح) → إعادة تسمية `app_offline` إلى `app_offline-used-2026-11-04-b.txt`. التوقف نحو 3 دقائق. التحقق: `/health` Healthy؛ `/en` و`/fr/Home/AboutUs` 200 مع hreflang؛ `/?culture=en` → 301 `/en`؛ `/User/Dashboard` → `/Account/Login?returnUrl=…`؛ `/Opportunity/Create` → `/Organization/Login`؛ `/for-organizations` و`/compass` بثلاث لغات؛ `/compass/me` و`/Admin/Analytics` يطلبان الدخول؛ `/api/team/requests` بلا مفتاح 401؛ sitemap 1,137 رابطًا بالبدائل؛ تقدير `Scholarships/XA` = 1,967 عضوًا؛ البوصلة لزائر أردني بكالوريوس: منح 12، تدريب 9، تطوع 7، وظائف 4 فرصة مؤهَّل لها الآن (ستزيد مع تعبئة بيانات الأهلية).

**بقي على المالك:** جولة `/Admin/Analytics` بعد يوم، `/Admin/PublishRequests`، إرسال طلب تجريبي من `/for-organizations` ببريدك، رموز Search Console/Bing في `Seo:` على الخادم، إبلاغ منصة الكتابة بتفعيل `requests` و`Unknown`، تعبئة `/Admin/Facets`، وتدوير كلمتي المرور. ملفات قديمة في `/site1` للحذف: `update-*.zip` و`app_offline-used-*.txt` ومكتبتا `TanvirArjel` و`NETCore.MailKit`.


**الإصدار الحي قبل النشر:** `e612ebd` (المرحلة 0، .NET 10) · **هذا الإصدار:** آخر commit على `main` · لا تغيير في الإطار.

## ما في هذه الدفعة

| # | البند | commit |
|---|---|---|
| — | واجهة الفريق تقبل `languageRequirement: "Unknown"` + تصحيح البرومبت | `3068aa7` |
| 1.4 | اللغة من الرابط: العربية بلا بادئة، `/en/…` و`/fr/…`، hreflang + x-default، sitemap بالبدائل، `?culture=` → 301 | `6f73848` |
| 1.3 + 1.5 | روابط حقيقية في الرئيسية؛ `returnUrl` عبر النافذة والتسجيل والدخول ورابط البريد؛ دخول تلقائي بعد التأكيد؛ `[Authorize]` للأفراد → `/Account/Login`؛ كوكي 30 يومًا؛ "تذكرني"؛ التطبيق المثبّت دخوله دائم | `98d2de4` |
| 1.2 | بوابة المنظمات `/for-organizations` + `PublishRequests` + `GET /api/team/requests` + تقدير المؤهَّلين + `/Admin/PublishRequests` | `387d373` |
| 1.6 | تحليلات داخلية بلا كوكيز (`/Admin/Analytics`) + وسم تحقق Search Console/Bing من الإعدادات | `ca63ff5` |
| 1.1 | «بوصلة»: `/compass` → `/compass/result` → `/compass/register` → `/compass/me` + `EligibilityProfiles` | آخر commit |

## قبل النشر

1. **SQL** في SQL Studio (بعد نسخة احتياطية): `Migrations/Sql/2026-11-publish-requests.sql` ثم `2026-11-analytics.sql` ثم `2026-11-eligibility-profiles.sql` — خمسة جداول جديدة فقط (`PublishRequests`، `PageViewDailyStats`، `TrafficSourceDailyStats`، `SiteEventDailyStats`، `EligibilityProfiles`) وتسجيل الهجرات الثلاث. كل سكربت دفعة واحدة (لا تعديل على أعمدة أُضيفت في الدفعة نفسها). بلا EXEC وبلا ENDPOINT.
2. إعدادات اختيارية على الخادم (`appsettings.production.json`): `"Seo": { "GoogleSiteVerification": "<الرمز من Search Console>", "BingSiteVerification": "<الرمز من Bing>" }` — فارغة = لا وسم. `BaseUrl` الموجود يُستعمل في رسائل المنظمات.
3. **أبلغ منصة الكتابة** أن `GET /api/team/requests` مفعَّلة (النص المحدَّث في `docs/PIPELINE-PROMPT-v2.md`، بند "طلبات المنظمات") وأن `languageRequirement` تقبل `Unknown`.

## خطوات النشر

1. SQL أعلاه (والموقع يعمل؛ الجدول الجديد لا يؤثر في الإصدار القديم).
2. `dotnet publish -c Release -o publish/new-<sha>` ثم حزمة الفرق بـsha256 مقابل `publish/site-files-current` (السكربت `make_deploy.py` في الجلسة السابقة؛ `CountryData.dll` يُستثنى). `app_offline.htm` → رفع → فك الضغط إلى `/site1` تحديدًا → إعادة تسمية `app_offline`.
3. **التحقق:**
   - `/health` = Healthy · `/en` و`/fr/Home/AboutUs` بلغتيهما · `/?culture=en` يحوّل 301 إلى `/en` · `/en/missing` صفحة 404 بالإنجليزية.
   - `/sitemap.xml` فيه `xhtml:link` و`/for-organizations` ×3.
   - `/for-organizations` بثلاث لغات، والأرقام غير صفرية، و`/for-organizations/estimate?type=Scholarships&country=XA` يرد JSON.
   - `/User/Dashboard` لزائر → `/Account/Login?returnUrl=…` (لا `/Organization/Login`).
   - `/api/team/requests` بلا مفتاح = 401، وبالمفتاح = `{"count":0,"items":[]}`.
   - `/Admin/Analytics` يفتح؛ بعد دقيقتين من التصفح العادي (من متصفح غير مسجَّل كمدير) تظهر مشاهدات.
   - `/compass` بثلاث لغات؛ أجب كزائر → `/compass/result` يعطي عددًا غير صفري (يعتمد على بيانات الأهلية المعبَّأة في `/Admin/Facets`) → `/compass/register` ببريد تجريبي → `/compass/me` تفتح كاملة.
4. **بعد النشر:** أرسل طلبًا تجريبيًا من `/for-organizations` ببريدك، تحقق من وصول رسالة الاستلام ومن ظهوره في `/Admin/PublishRequests` وجرس المدير، ثم اربطه بفرصة منشورة وتحقق من رسالة "نُشرت فرصتكم". اختبر أيضًا تأكيد بريد لحساب جديد: يجب أن يسجّل الدخول تلقائيًا من الرابط.
5. حدّث `publish/site-files-current`، ثم `git push`.

## ما يتغيّر للمستخدمين

- **الروابط:** الإنجليزية والفرنسية تحت `/en/` و`/fr/`؛ الروابط القديمة بـ`?culture=` تتحوّل تلقائيًا (301). الزائر العائد يُحوَّل من الرئيسية فقط إلى لغته الأخيرة.
- **الزوار:** بطاقات الرئيسية روابط حقيقية؛ بعد الدخول أو التسجيل أو تأكيد البريد يعودون إلى ما كانوا يفتحونه.
- **المنظمات:** صفحة `/for-organizations` لطلب النشر بلا حساب، ورسائل استلام ونشر بلغتهم.
- **المدير:** `/Admin/PublishRequests` وإشعار جرس/دفع لكل طلب جديد، وعدّاد في لوحة التحكم.

## للتراجع

الحزمة السابقة `publish/site-files-current` (e612ebd) ترجع بالرفع نفسه. جدول `PublishRequests` لا يضر الإصدار القديم ويمكن تركه.
