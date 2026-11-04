# نشر المرحلة 2 (Google بعمق: 2.1 + 2.2 + 2.3 + 2.4) — قائمة التحقق

**الإصدار الحي قبل النشر:** `d1e19bc` (المرحلة 1) · **هذا الإصدار:** `16fa043` · لا تغيير في الإطار.

## ✅ نُشر — 4 نوفمبر 2026 (الإصدار الحي الآن `16fa043`)

- نسخة احتياطية من القاعدة، ثم `2026-11-landing-pages.sql` في SQL Studio (`has_landing_pages` ≠ NULL، الهجرة مسجّلة).
- حزمة الفرق: 3 ملفات (`Foras_Khadra.dll`, `Foras_Khadra.pdb`, `wwwroot/css/compass.css.gz`) مع `app_offline.htm` في رفع واحد → فك إلى `/site1` → إعادة تسمية `app_offline` إلى `app_offline-used-2026-11-04-c.txt`. `publish/site-files-current` مُحدَّث.
- **الدفعة الأولى (36 صفحة):** زر «استيراد الدفعة الأولى» يحتاج تسجيل دخول المدير على الإنتاج، فأُدرجت عبر `Migrations/Sql/2026-11-landing-pages-seed.sql` في SQL Studio (النتيجة: 36 صفحة، 36 منشورة). الملف مولَّد من `LandingPageSeed.All` ومُتحقَّق من تطابقه حقلًا حقلًا مع الكود على LocalDB داخل معاملة مُلغاة. الضغط على الزر لاحقًا يعيد «لا جديد».
- **التحقق (كله ناجح):** `/health` Healthy · `/opportunities/jobs` و`/en/opportunities/scholarships` 200 بعنوان وcanonical · `/Home/Jobs` و`/Home/AllOpportunities?type=Scholarships` → 301 · `/opportunities/trainings` → 301 إلى `internships` · `?funding=FullyFunded` فيه `noindex,follow` والعدّاد · الوظيفة 338 فيها `JobPosting` داخل `<script type="application/ld+json">` ورابط `/go/338` ظاهر للزائر ويحوّل 302 مباشرة · sitemap فيه الأنواع السبعة · `/opportunities/egypt` (292) و`jordan` (270) و`arab-world` (234) 200 مع `FAQPage` وبلا noindex · `fully-funded` و`no-ielts` و`scholarships-in-germany` و`women` 200 مع `FAQPage` لكن noindex (0 فرص: الحقول المهيكلة لم تُملأ بعد) · `/opportunities/not-a-page` 404.
- **ما بقي للمالك:** اختبار Rich Results على وظيفة وصفحة نية؛ ملء `/Admin/Facets` وتزويد منصة الكتابة بالبرومبت v2 حتى تمتلئ صفحات التمويل/آيلتس/الاستضافة/النساء؛ تحرير نصوص الدفعة الأولى القالبية من `/Admin/LandingPages`؛ حذف `app_offline-used-*` والحزم القديمة من `/site1`.

## ما في هذه الدفعة

| # | البند | commit |
|---|---|---|
| 2.1 + 2.2 + 2.4 | صفحات الأنواع `/opportunities/{slug}` (301 من القديمة)، فلاتر وترتيب حقيقية من الحقول المهيكلة مع noindex للمفلتر، JobPosting صحيح + رابط التقديم ظاهر للزائر في الوظائف/التدريب/التطوع، إصلاح `ld+json` | `fbaf1b7`, `75253c2` |
| 2.3 | صفحات النية `LandingPages` + `/Admin/LandingPages` + الدفعة الأولى (36 صفحة) + `FAQPage` | آخر commit |

## قبل النشر

1. **SQL** في SQL Studio (بعد نسخة احتياطية): `Migrations/Sql/2026-11-landing-pages.sql` — جدول واحد جديد `LandingPages` وتسجيل الهجرة `20261104161802_LandingPages`. دفعة واحدة. بلا EXEC وبلا ENDPOINT.
2. لا إعدادات جديدة على الخادم.

## خطوات النشر

1. SQL أعلاه (والموقع يعمل).
2. `dotnet publish -c Release -o publish/new-<sha>` → حزمة الفرق بـsha256 مقابل `publish/site-files-current` (`make_deploy.py` في scratchpad الجلسة؛ `CountryData.dll` يُستثنى). `app_offline.htm` + الحزمة معًا في رفع واحد → فك الضغط إلى `/site1` (تحقق من قيمة الوجهة) → إعادة تسمية `app_offline`.
3. **بعد النشر مباشرة:** `/Admin/LandingPages` → «استيراد الدفعة الأولى» (36 صفحة). الصفحات تظهر `noindex` حتى يبلغ عدد فرصها المفتوحة 5؛ الجدول في اللوحة يعرض العدد والحالة لكل صفحة.
4. **التحقق:**
   - `/health` = Healthy.
   - `/opportunities/jobs` و`/en/opportunities/scholarships` 200 بعنوان ومقدمة؛ `/Home/Jobs` و`/Home/AllOpportunities?type=Jobs` → 301 إلى `/opportunities/jobs`؛ `/opportunities/trainings` → 301 إلى `/opportunities/internships`.
   - `/opportunities/scholarships?funding=FullyFunded` فيه `<meta name="robots" content="noindex,follow">` ورقائق الفلاتر والعدّاد.
   - `/opportunities/egypt` و`/opportunities/fully-funded` و`/opportunities/no-ielts` 200 مع الأسئلة الشائعة؛ `/opportunities/not-a-page` 404.
   - صفحة وظيفة أو تدريب مفتوحة: `<script type="application/ld+json">` بـ`JobPosting` (لا `ld&#x2B;json`)، وزر «قدّم الآن» ظاهر للزائر ويفتح `/go/{id}` الذي يحوّل مباشرة.
   - `/sitemap.xml` فيه `/opportunities/{slug}` للأنواع السبعة ×3 لغات، وصفحات النية التي بلغت 5 فرص (تُحسب كل ساعة).
   - اختبار Google: https://search.google.com/test/rich-results على صفحة وظيفة وصفحة نية (JobPosting وFAQPage).
5. حدّث `publish/site-files-current`، ثم `git push`.

## ما يتغيّر للمستخدمين

- **الزوار:** يمكنهم التقديم على الوظائف والتدريب والتطوع بلا حساب (كما يطلب Google for Jobs)؛ بقية الأنواع كما كانت. صفحات الأنواع والنية بعناوين ومقدمات؛ فلاتر حقيقية وترتيب وبحث ظاهر.
- **المدير:** `/Admin/LandingPages` لإضافة صفحات نية جديدة وتحرير نصوص الدفعة الأولى (القالبية) لتصير بشرية أكثر.
- **Google:** canonical واضح لكل صفحة نوع/نية، noindex لكل تركيبة فلاتر، JobPosting نظيف، FAQPage على صفحات النية.

## للتراجع

الحزمة السابقة (`d1e19bc`) ترجع بالرفع نفسه. جدول `LandingPages` لا يضر الإصدار القديم ويمكن تركه. الروابط القديمة `/Home/Jobs…` تعود للعمل تلقائيًا مع الإصدار القديم.
