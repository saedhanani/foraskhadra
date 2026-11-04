# نشر المرحلة 1 (الدفعة الأولى: 1.2 + 1.3 + 1.4 + 1.5) — قائمة التحقق

**الإصدار الحي قبل النشر:** `e612ebd` (المرحلة 0، .NET 10) · **هذا الإصدار:** آخر commit على `main` · لا تغيير في الإطار.

## ما في هذه الدفعة

| # | البند | commit |
|---|---|---|
| — | واجهة الفريق تقبل `languageRequirement: "Unknown"` + تصحيح البرومبت | `3068aa7` |
| 1.4 | اللغة من الرابط: العربية بلا بادئة، `/en/…` و`/fr/…`، hreflang + x-default، sitemap بالبدائل، `?culture=` → 301 | `6f73848` |
| 1.3 + 1.5 | روابط حقيقية في الرئيسية؛ `returnUrl` عبر النافذة والتسجيل والدخول ورابط البريد؛ دخول تلقائي بعد التأكيد؛ `[Authorize]` للأفراد → `/Account/Login`؛ كوكي 30 يومًا؛ "تذكرني"؛ التطبيق المثبّت دخوله دائم | `98d2de4` |
| 1.2 | بوابة المنظمات `/for-organizations` + `PublishRequests` + `GET /api/team/requests` + تقدير المؤهَّلين + `/Admin/PublishRequests` | هذا الـcommit |

## قبل النشر

1. **SQL** في SQL Studio (بعد نسخة احتياطية): `Migrations/Sql/2026-11-publish-requests.sql` — جدول واحد جديد `PublishRequests` وتسجيل الهجرة `20261104133908_PublishRequests`. دفعة واحدة تكفي (لا تعديل على أعمدة أُضيفت في الدفعة نفسها). بلا EXEC وبلا ENDPOINT.
2. لا إعدادات جديدة على الخادم. `BaseUrl` الموجود يُستعمل في رسائل المنظمات.
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
4. **بعد النشر:** أرسل طلبًا تجريبيًا من `/for-organizations` ببريدك، تحقق من وصول رسالة الاستلام ومن ظهوره في `/Admin/PublishRequests` وجرس المدير، ثم اربطه بفرصة منشورة وتحقق من رسالة "نُشرت فرصتكم". اختبر أيضًا تأكيد بريد لحساب جديد: يجب أن يسجّل الدخول تلقائيًا من الرابط.
5. حدّث `publish/site-files-current`، ثم `git push`.

## ما يتغيّر للمستخدمين

- **الروابط:** الإنجليزية والفرنسية تحت `/en/` و`/fr/`؛ الروابط القديمة بـ`?culture=` تتحوّل تلقائيًا (301). الزائر العائد يُحوَّل من الرئيسية فقط إلى لغته الأخيرة.
- **الزوار:** بطاقات الرئيسية روابط حقيقية؛ بعد الدخول أو التسجيل أو تأكيد البريد يعودون إلى ما كانوا يفتحونه.
- **المنظمات:** صفحة `/for-organizations` لطلب النشر بلا حساب، ورسائل استلام ونشر بلغتهم.
- **المدير:** `/Admin/PublishRequests` وإشعار جرس/دفع لكل طلب جديد، وعدّاد في لوحة التحكم.

## للتراجع

الحزمة السابقة `publish/site-files-current` (e612ebd) ترجع بالرفع نفسه. جدول `PublishRequests` لا يضر الإصدار القديم ويمكن تركه.
