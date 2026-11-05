# نشر المرحلة 3 (الثقة وتقرير المنظمات: 3.1 → 3.5) — قائمة التحقق

**الإصدار الحي قبل النشر:** `16fa043` (المرحلة 2) · **هذا الإصدار:** `d41b125` (+ إصلاح `a87cfef`) · لا تغيير في الإطار.

## ✅ نُشر — 5 نوفمبر 2026 (الإصدار الحي `a87cfef` = المرحلة 3 `d41b125` + إصلاح معاينة التضمين)

- نسخة احتياطية من القاعدة، ثم `2026-11-trust.sql` (has_tier/has_reports/has_follows كلها موجودة) ثم `2026-11-trust-data.sql` في تنفيذ منفصل (58 منظمة بدرجة «مسجَّلة»، لا موثَّقة بعد).
- حزمة الفرق: 27 ملفًا (DLL + PDB + ملفات الموارد ×3 + brand.css + opportunity-deatails.css + site.js + embed.js + الشارات ×3 مع نسخ gz/br) مع `app_offline.htm` → فك إلى `/site1` → إعادة تسمية `app_offline` إلى `app_offline-used-2026-11-05.txt`. `publish/site-files-current` مُحدَّث إلى `d41b125`.
- **التحقق على الإنتاج (كله ناجح):** `/health` Healthy · الفرصة 338 فيها شريط الثقة الأربعة ونافذة البلاغ ورابط التخطي و`main#main` · `/embed/opportunities?type=Jobs&limit=3` 200 بثلاثة عناصر بلا `X-Frame-Options` وبـ`Cache-Control: public,max-age=600` · `/embed` و`/en/embed` و`/js/embed.js` والشارات 200 · `/Admin/Reports` و`/Organization/Report` تحوّل إلى الدخول · `/en/Home/Articles`: «All rights reserved © 2026» و5 أسهم `dir-flip` وbrand.css يحمل كتلة الوصول · صفحة المنظمة 19 فيها زر المتابعة وقسم فرصها · دخول خاطئ بالإنجليزية: «Incorrect email or password».
- **خلل اكتُشف بعد النشر:** معاينة `/embed` لا تحمّل الودجت لأن CSP الموقع كان `frame-src https://www.google.com` فقط (يمنع الموقع من تضمين نفسه). الإصلاح `a87cfef` (`frame-src 'self' …` + إزالة `loading="lazy"` من المعاينة) نُشر في الجولة الثانية (DLL + PDB، `app_offline` → `app_offline-used-2026-11-05-b.txt`) بعد أن رفض نظام الأذونات الرفع مرة ثم تعطّلت لوحة الاستضافة دقائق؛ **التحقق:** `/embed` على الإنتاج يعرض المعاينة بستّ بطاقات وارتفاع تلقائي 941px، و`frame-src 'self' https://www.google.com` في الترويسة، والودجت ما زال بلا `X-Frame-Options`. `publish/site-files-current` = `a87cfef`.
- **ما بقي للمالك:** تجربة Rich Results على فرصة؛ ضبط درجات التوثيق للمنظمات الحقيقية من `/Admin/OrganizationDetails/{id}`؛ حذف `update-part1.zip` و`update-a87cfef.zip` و`app_offline-used-*` من `/site1`.

## ما في هذه الدفعة

| # | البند | ما يراه المستخدم |
|---|---|---|
| 3.1 | شريط الثقة على صفحة الفرصة: «تم التحقق بتاريخ» (`LastCheckedAt ?? ReviewedAt ?? UpdatedAt ?? PublishDate`)، «المصدر الرسمي» (`SourceUrl ?? ApplyLink`)، «التقديم مجاني — لا تدفع لوسيط» أو تحذير الرسوم (`HasApplicationFee`)، شارة «منظمة موثَّقة/شريكة»، زر «أبلغ عن مشكلة» (نافذة `dialog`). بلاغان مفتوحان من مُبلِّغَين مختلفَين يوقفان زر التقديم و`/go/{id}` ويُشعران المدير. `/Admin/Reports` للمعالجة. درجات التوثيق `Organization.VerificationTier` (None/Registered/Verified/Partner) من `/Admin/OrganizationDetails/{id}` | الزائر والعضو والمدير |
| 3.2 | `/Organization/Report?days=7|30` تقرير الوصول (مشاهدات، نقرات، نسبة النقر، محفوظات، متابعون، يوم بيوم، جدول الفرص) + CSV + «تنزيل PDF» (طباعة المتصفح). «تابع هذه المنظمة» على صفحة الفرصة وصفحة المنظمة العامة (التي صارت تعرض فرصها)؛ المتابع يُشعَر عند نشر فرصة جديدة (إشعار داخلي + دفع) | المنظمة والعضو |
| 3.3 | `/embed` مولّد أداة التضمين (قصاصة `<div class="fk-embed">` + `/js/embed.js`) و`/embed/opportunities` الودجت داخل iframe (بلا X-Frame-Options، `frame-ancestors *` لهذا المسار فقط)، وشارة «نُشر على فرص خضراء» ×3 لغات (`/images/badge-published-{ar,en,fr}.svg`) | الجامعات والمنظمات |
| 3.4 | تباين الأزرار البرتقالية (نص داكن)، رابط تخطٍّ، `aria-expanded`/`aria-controls` على كل قوائم الهيدر وEscape يغلقها، الأسهم تنعكس في en/fr (`dir-flip`)، «© {السنة}» من resx بمفتاح صحيح، رسائل الدخول من resx (3 لغات)، فك ترميز الأوصاف في القوائم | الجميع |
| 3.5 | صفحة الخطأ العام بثلاث لغات، حالات فارغة لقوائم الإدارة والمقالات، حالة تحميل على أزرار الإرسال (12 نموذجًا) ومكان الإشعارات | الجميع |

## قبل النشر

1. **SQL** في SQL Studio (بعد نسخة احتياطية)، **تنفيذان منفصلان**:
   - `Migrations/Sql/2026-11-trust.sql` — عمود `Organizations.VerificationTier` + جدولا `OpportunityReports` و`OrganizationFollows` + تسجيل الهجرة `20261104205520_Stage3Trust`.
   - ثم `Migrations/Sql/2026-11-trust-data.sql` — الموثَّقة أصلًا تأخذ درجة 2، وبقية المنظمات ذات الحساب درجة 1.
2. لا إعدادات جديدة على الخادم. لا تغيير في حقول الفرصة (لا برومبت جديد لمنصة الكتابة).

## خطوات النشر

1. SQL أعلاه (والموقع يعمل).
2. `dotnet publish -c Release -o publish/new-<sha>` → حزمة الفرق بـsha256 مقابل `publish/site-files-current` (`make_deploy.py`). المتوقع: `Foras_Khadra.dll/.pdb` + `wwwroot/css/brand.css(.gz/.br)` + `opportunity-deatails.css` + `js/site.js` + `js/embed.js` + `images/badge-published-*.svg`. `app_offline.htm` + الحزمة معًا → فك الضغط إلى `/site1` → إعادة تسمية `app_offline`.
3. **التحقق:**
   - `/health` = Healthy.
   - صفحة فرصة مفتوحة: شريط الثقة (تاريخ التحقق، المصدر الرسمي، «التقديم مجاني»)، زر «أبلغ عن مشكلة» يفتح النافذة ويرسل بلا إعادة تحميل.
   - `/Admin/Reports` (المدير) يعرض البلاغات؛ «عولجت كلها» يعيد زر التقديم.
   - `/Admin/OrganizationDetails/{id}`: قائمة «درجة التوثيق»؛ المنظمة الموثَّقة تحمل الشارة على صفحة الفرصة و`/Home/DetailsOrganization/{id}` (التي تعرض فرصها وزر المتابعة).
   - `/Organization/Report` (حساب منظمة) يعرض الأرقام و`?format=csv` ينزّل ملفًا.
   - `/embed` يعرض المولّد والمعاينة؛ `/embed/opportunities?type=Scholarships&limit=3` يعيد 200 **بلا** `X-Frame-Options` ومع `frame-ancestors *`؛ `/js/embed.js` و`/images/badge-published-ar.svg` 200.
   - `/en/Home/AllOpportunities`: أسهم الترقيم تشير للاتجاه الصحيح؛ تذييل الصفحة يعرض «© 2026» بلغة الصفحة.
   - Escape يغلق قائمة اللغة/الحساب؛ Tab من أول الصفحة يُظهر «تخطَّ إلى المحتوى».
   - `/Account/Login` ببيانات خاطئة: رسالة «Incorrect email or password» في `/en/`.
4. حدّث `publish/site-files-current`، ثم `git push`.

## ما يتغيّر للمستخدمين

- **الزوار:** يعرفون متى تُحقِّق من الفرصة ومن أين، وأن التقديم مجاني؛ يمكنهم الإبلاغ عن رابط معطّل أو احتيال دون حساب.
- **الأعضاء:** يتابعون منظمة ويصلهم جديدها.
- **المنظمات:** تقرير وصول حقيقي (7/30 يومًا) بـCSV وPDF، عدد المتابعين، شارة وودجت لموقعها.
- **المدير:** بلاغات الزوار ودرجات التوثيق في اللوحة.

## للتراجع

الحزمة السابقة (`16fa043`) ترجع بالرفع نفسه. الجدولان والعمود الجديد لا تضر الإصدار القديم.
