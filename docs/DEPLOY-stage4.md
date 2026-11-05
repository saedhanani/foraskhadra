# نشر المرحلة 4 (النمو والمجتمع والإيرادات: 4.1 → 4.8) — قائمة التحقق

**الإصدار الحي قبل النشر:** `a87cfef` (المرحلة 3) · **هذا الإصدار:** آخر commit على `main` · لا تغيير في الإطار.

## ما في هذه الدفعة

| # | البند | ما يراه المستخدم |
|---|---|---|
| 4.1 | **أدلة التقديم** `/guides` و`/guides/{slug}` (`ApplicationGuide` + `Data/ApplicationGuideSeed`: 8 برامج — chevening, daad-epos, erasmus-mundus, turkiye-burslari, mext, fulbright, csc, mastercard-foundation) مرتبطة بـ`ProgramKey`: بطاقة الدليل على صفحة كل فرصة من البرنامج، رابطه في «قريبًا» بالبوصلة، ومهامه تُضاف إلى رفيق التقديم («حمّل القائمة إلى متابعتي»). لوحة `/Admin/Guides` + «استيراد الدفعة الأولى» | الزائر والعضو |
| 4.2 | **صفحة الأثر** `/impact` (أرقام حيّة من الموقع + `ImpactStats`) و**قصص النجاح** من «مقبول» في المتابعة (`/stories/new`، موافقة صريحة، اسم العرض يختاره العضو) بعد اعتماد `/Admin/Stories` | الجميع |
| 4.3 | **قنوات نملكها:** خلاصات Atom `/feed/opportunities.xml` (+أي فلتر)، `/feed/{نوع}.xml`، `/feed/{صفحة نية}.xml`، بثلاث لغات، مع autodiscovery في كل صفحة؛ صفحة «تنتهي خلال 7/15 يومًا» `/opportunities/closing-soon` (مفهرسة)؛ تقويم webcal شخصي من «فرصي» (`/calendar/{token}.ics`)؛ قناة تيليجرام آلية (`Services/TelegramPoster`) معطَّلة حتى تُضبط `Telegram:BotToken` و`Telegram:ChannelId` | الجميع |
| 4.4 | **السفراء الجامعيون:** `/ambassador` (6 مهام شهرية بنقاط، الدعوات تُعتمد آليًا، شارات برونزي/فضي/ذهبي، شهادة للطباعة)، `/ambassadors/universities` لوحة الجامعات، مراجعة `/Admin/AmbassadorTasks` | العضو |
| 4.5 | **الإيرادات بلا إعلانات:** `Opportunity.IsSponsored` + `SponsoredUntil` (تظهر أولًا بعلامة «إعلان مموَّل»)، «عزّز للمؤهَّلين» (`BoostService` عبر CompassMatcher، إشعار + دفع، سجل `OpportunityBoosts`)، صفحة الباقات `/for-organizations/packages`، لوحة `/Admin/Promote` | المنظمات والمدير |
| 4.6 | **أسئلة وأجوبة** على كل فرصة: منسَّقة من المدير بثلاث لغات أو من الأعضاء بعد المراجعة (`/Admin/Questions`)، `FAQPage` على صفحة الفرصة، «N يتابعون» | الجميع |
| 4.7 | **Google Play:** `/.well-known/assetlinks.json` من الإعدادات `Android:PackageName` و`Android:Sha256Fingerprints`؛ الخطوات في `docs/ANDROID.md` (بقرار المالك، 25$) | المالك |
| 4.8 | دفعة سنوية للمسارات الخضراء مع RCOY MENA — تشغيلي؛ بند الرعاية في صفحة الباقات | المالك |

## قبل النشر

1. **SQL** في SQL Studio (بعد نسخة احتياطية)، تنفيذ واحد: `Migrations/Sql/2026-11-growth.sql` — عمودا `Opportunities.IsSponsored/SponsoredUntil` + جداول `ApplicationGuides`، `SuccessStories`، `OpportunityQuestions`، `AmbassadorTaskSubmissions`، `OpportunityBoosts` + تسجيل الهجرة `20261105083603_Stage4Growth`.
2. لا إعدادات إلزامية جديدة. اختياري: `Telegram:BotToken` + `Telegram:ChannelId` لتفعيل القناة؛ `Android:*` لتطبيق Play؛ `Site:BaseUrl` (الافتراضي https://www.foraskhadra.com).
3. لا تغيير في حقول الإرسال لمنصة الكتابة؛ `isSponsored` للقراءة فقط (ملاحظة في `docs/PIPELINE-PROMPT-v2.md`).

## خطوات النشر

1. SQL أعلاه (والموقع يعمل).
2. `dotnet publish -c Release -o publish/new-<sha>` → حزمة الفرق مقابل `publish/site-files-current` (`make_deploy.py`). المتوقع: DLL/PDB + ملفات الموارد + `brand.css` + `site.js` (إن تغيّر). `app_offline.htm` + الحزمة → فك إلى `/site1` → إعادة تسمية `app_offline`.
3. **بعد النشر مباشرة:** `/Admin/Guides` → «استيراد الدفعة الأولى» (8 أدلة). إن لم يتوفر دخول المدير، يُولَّد SQL من `ApplicationGuideSeed` كما فُعل لصفحات النية.
4. **التحقق:**
   - `/health` Healthy.
   - `/feed/opportunities.xml` و`/feed/jobs.xml` و`/en/feed/egypt.xml` تعيد Atom صالحًا (`Content-Type: application/atom+xml`) وكل صفحة فيها `<link rel="alternate" type="application/atom+xml">`.
   - `/opportunities/closing-soon` و`?within=15` 200 بعنوانهما وبلا `noindex`، و`/Home/AllOpportunities?within=15` يعمل.
   - `/guides` (8 أدلة بعد الاستيراد) و`/guides/chevening` 200 مع `FAQPage`؛ فرصة لها `programKey` تعرض بطاقة الدليل.
   - `/impact` 200 بالأرقام؛ `/stories/new` و`/ambassador` تحوّلان الزائر إلى الدخول؛ `/ambassadors/universities` 200.
   - `/for-organizations/packages` 200؛ `/.well-known/assetlinks.json` يعيد `[]`.
   - صفحة فرصة: قسم «أسئلة وأجوبة» ظاهر؛ المدير يضيف سؤالًا منسَّقًا من `/Admin/Questions` فيظهر مع `FAQPage`.
   - `/Admin/Promote`: تفعيل الإبراز على فرصة → تظهر أولًا بعلامة «إعلان مموَّل» في صفحة نوعها.
5. حدّث `publish/site-files-current`، ثم `git push`.

## ما يتغيّر للمستخدمين

- **الزوار:** أدلة تقديم بالعربية للبرامج الكبرى، صفحة «تنتهي قريبًا»، خلاصات RSS، أسئلة وأجوبة على الفرص، صفحة الأثر.
- **الأعضاء:** قائمة مهام الدليل داخل رفيق التقديم، تقويم مواعيد شخصي، لوحة السفير والشهادة، مشاركة قصة النجاح.
- **المنظمات:** باقات واضحة بلا إعلانات؛ الإبراز والتعزيز بموافقة المدير.
- **المدير:** خمس لوحات جديدة تحت لوحة التحكم.

## للتراجع

الحزمة السابقة (`a87cfef`) ترجع بالرفع نفسه. الجداول والأعمدة الجديدة لا تضر الإصدار القديم.
