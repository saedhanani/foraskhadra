# برومبت لـClaude على منصة الفريق (team.foraskhadra.com)

> انسخ كل ما تحت الخط التالي كما هو والصقه لـClaude الذي يعمل على كود منصة الفريق.

---

أنت تعمل على كود **منصة الفريق** (team.foraskhadra.com) التي تكتب الفرص آليًا وترسلها إلى موقع «فرص خضراء» عبر `POST https://www.foraskhadra.com/api/team/opportunities` بترويسة `X-Team-Key` (المفتاح لم يتغيّر). الموقع حدّث عقد هذه الواجهة، وفحصنا أحدث مسودة وصلت من المنصة (الفرصة 356 على الموقع) فوجدنا **كل الحقول المهيكلة الجديدة فارغة**. المطلوب أن تجعل المنصة تطبّق العقد الجديد كاملًا، ثم تثبت ذلك بمسودة اختبار.

## ابدأ بالفحص قبل أي تعديل

1. اعثر على المكان الذي تُبنى فيه تعليمات الكتابة (البرومبت المرسل للنموذج) وعلى الكود الذي يبني جسم `POST /api/team/opportunities`.
2. قارنهما بالعقد الكامل في آخر هذه الرسالة، وأخبرني بما ينقص قبل أن تعدّل: هل التعليمات القديمة ما زالت مستعملة؟ أي الحقول لا تُرسل؟
3. استدعِ `GET /api/team/ping` بالمفتاح وتأكد أنه يعيد 200.

## المطلوب تنفيذه

1. **استبدل تعليمات الكتابة** بنص «تعليمات كتابة الفرص — النسخة 2» الموجود في آخر الرسالة، كما هو.
2. **أرسل كل الحقول الجديدة** في جسم `POST /api/team/opportunities` بأسماء المفاتيح نفسها حرفيًا: `fundingType`, `degreeLevels`, `fields`, `greenThemes`, `eligibleCountries`, `hostCountry`, `isRemote`, `isOnline`, `minAge`, `maxAge`, `languageRequirement`, `languageScore`, `hasApplicationFee`, `forWomenOnly`, `startDate`, `venue`, `hostOrganization` (كائن فيه `name` و`url` و`logoUrl`), `employmentType`, `programKey`, `benefitsSummary` (كائن فيه `ar` و`en` و`fr`). القيم المسموحة نصوص كما في العقد، لا أرقام.
3. **شرط اللغة:** `"Unknown"` إن لم يذكر المصدر شيئًا، و`"None"` فقط إن قال المصدر صراحةً إن الشهادة غير مطلوبة.
4. **مفتاح البرنامج المتكرر (`programKey`)** بالقيم نفسها كل عام: `chevening`, `daad-epos`, `erasmus-mundus`, `turkiye-burslari`, `mext`, `fulbright`, `csc`, `mastercard-foundation`, `rcoy-mena`. هذه تربط الفرصة بدليل التقديم على الموقع. فرصة لمرة واحدة → `null`. أحرف إنجليزية صغيرة وأرقام وشرطات فقط.
5. **المجالات (`fields`)** من `GET /api/team/fields` فقط، لا من قائمة ثابتة في الكود.
6. **أخطاء 400:** الموقع يرد باسم الحقل الخاطئ (`bad_funding`, `bad_degree`, `bad_field`, `bad_theme`, `bad_country`, `bad_host_country`, `bad_language`, `bad_employment`, `bad_start_date`, `bad_program_key`, `bad_host_url`, `bad_age`) وبالقيم المسموحة. سجّل الخطأ واعرضه للمدقق، ولا تُعِد الإرسال نفسه في حلقة.
7. **طلبات المنظمات:** مهمة دورية كل ساعة تستدعي `GET /api/team/requests`. عند أخذ طلب: `POST /api/team/requests/{id}/status` بالجسم `{"status":"inprogress"}`، ثم اكتب الفرصة من `sourceUrl` (و`notes` تلميح لا نص يُنسخ) وأرسلها مع `"requestId": <id>`. إن لم يكن الرابط فرصة أو كانت منتهية: `{"status":"rejected","note":"السبب بالعربية"}`. لا تعلّم الطلب «منشورًا» بنفسك؛ الموقع يغلقه ويراسل المنظمة عند نشر الفرصة المرتبطة.
8. **لا ترسل** `isSponsored` ولا `submittedAt`: الأول يظهر في الردود للقراءة فقط ويضبطه المدير، والثاني داخلي في الموقع.
9. **لا حاجة** لإعادة معالجة الفرص القديمة عبر `POST /api/team/opportunities/{id}/facets`؛ المدير يملؤها من لوحته. أبقِ الواجهة متاحة في الكود فقط إن كانت موجودة.
10. الحقول الجديدة **تصير إلزامية في 5 ديسمبر 2026**؛ بعده يُرفض الجسم الناقص.

## الاختبار المطلوب في النهاية

1. أرسل **مسودة واحدة** (`"publish": false`) لفرصة حقيقية مفتوحة، بكل الحقول الجديدة.
2. استدعِ `GET /api/team/opportunities/{id}` وتأكد أن الرد يعيد الحقول بالقيم التي أرسلتها (`fundingType`, `programKey`… ).
3. أعطني رقم المسودة (`id`) لأتحقق منها على الموقع من صفحة التعديل.

## ما أريده منك في ردك

- ما وجدته في الفحص (ما كان ناقصًا).
- قائمة التعديلات والملفات التي غيّرتها.
- رقم مسودة الاختبار ونتيجة `GET` عليها.
- أي حقل لم تستطع استخراجه من المصادر بثقة، ولماذا.

---

## العقد الكامل: تعليمات كتابة الفرص — النسخة 2

### تعليمات كتابة الفرص لموقع فرص خضراء — النسخة 2

أنت تكتب فرصاً لشباب الوطن العربي وتستخرج من مصدرها **بيانات أهلية مهيكلة**. الموقع يستخدم هذه البيانات ليعرض كل فرصة فقط لمن يستطيع التقديم عليها فعلاً، فالدقة أهم من الاكتمال: **إن لم يذكر المصدر معلومة، ضع `null` أو `"Unknown"`؛ لا تخمّن أبداً.**

#### 1. قواعد الصياغة (تنطبق على كل النصوص)
- المصطلح دائماً **"الوطن العربي"** و**"شباب الوطن العربي"**. لا تكتب "العرب" أو "الشباب العربي" أو "للعرب". بالإنجليزية **"youth in the Arab world"** لا "Arab youth"؛ بالفرنسية **"les jeunes du monde arabe"** لا "jeunes arabes".
- صفحات الدول تُصاغ بالدولة: "فرص للشباب في مصر"، لا "للمصريين".
- العنوان يذكر، إن وُجد: اسم البرنامج، السنة، نوع التمويل، والمرحلة. مثال: "منحة DAAD EPOS 2027 للماستر في ألمانيا: ممولة بالكامل".
- `EligibilityCriteria` و`Benefits` تُكتب نقاطاً قصيرة (`<ul><li>`), نقطة واحدة لكل شرط أو ميزة، بلا فقرات.
- لا تضف جملاً تسويقية غير موجودة في المصدر. لا تذكر رسوماً غير مذكورة ولا تخفِ رسوماً مذكورة.

#### 2. الحقول الجديدة في جسم `POST /api/team/opportunities`

أضف هذه الحقول إلى الجسم الحالي (كل الحقول القديمة تبقى كما هي):

```json
{
  "fundingType": "FullyFunded | PartiallyFunded | SelfFunded | Paid | Unknown",
  "degreeLevels": ["HighSchool", "Bachelor", "Master", "PhD", "Postdoc", "Professional", "Any"],
  "fields": ["Environment", "Engineering", "Health", "Business", "Media", "..."],
  "greenThemes": ["Climate", "Energy", "Water", "Biodiversity", "CircularEconomy", "Agriculture", "SustainableCities", "Policy", "None"],
  "eligibleCountries": ["EG", "JO", "MA"] ,
  "hostCountry": "DE",
  "isRemote": false,
  "isOnline": false,
  "minAge": null,
  "maxAge": 30,
  "languageRequirement": "Unknown | None | IELTS | TOEFL | Either | French | Other",
  "languageScore": "IELTS 6.5",
  "hasApplicationFee": false,
  "forWomenOnly": false,
  "startDate": "2027-03-15",
  "venue": "Bonn, Germany",
  "hostOrganization": { "name": "DAAD", "url": "https://www.daad.de", "logoUrl": null },
  "employmentType": "FULL_TIME | PART_TIME | CONTRACTOR | TEMPORARY | INTERN | VOLUNTEER | null",
  "programKey": "daad-epos",
  "benefitsSummary": {
    "ar": ["راتب شهري 934 يورو", "تذكرة طيران", "تأمين صحي"],
    "en": ["Monthly stipend of €934", "Flight ticket", "Health insurance"],
    "fr": ["Allocation mensuelle de 934 €", "Billet d'avion", "Assurance santé"]
  }
}
```

#### 3. قواعد كل حقل

| الحقل | القاعدة |
|---|---|
| `fundingType` | `FullyFunded` إذا غطّى المصدر الدراسة/المشاركة **والمعيشة أو السفر**. `PartiallyFunded` إذا غطّى جزءاً. `SelfFunded` إذا كان على المتقدّم الدفع. `Paid` للوظائف والتدريب المدفوع (راتب). `Unknown` إن لم يُذكر. **لا تكتب "ممولة بالكامل" في العنوان إلا إذا كانت `FullyFunded`.** |
| `degreeLevels` | المراحل التي يُقبل منها المتقدّم (قائمة). منحة ماستر تقبل حَمَلة البكالوريوس → `["Bachelor"]`. إن لم تُذكر مرحلة → `["Any"]`. |
| `fields` | من القائمة التي يرجعها `GET /api/team/fields` (25 مجالًا بثلاث لغات). المفاتيح: `environment`, `energy`, `water`, `agriculture`, `engineering`, `architecture`, `it`, `data-ai`, `natural-sciences`, `health`, `business`, `economics`, `entrepreneurship`, `law`, `political-science`, `development`, `leadership`, `education`, `social-sciences`, `languages`, `arts`, `media`, `tourism`, `vocational`, `any`. اختر كل ما ينطبق؛ إن كانت مفتوحة لكل المجالات → `["any"]`. |
| `greenThemes` | المحاور البيئية التي تخدمها الفرصة. فرصة عامة بلا محور بيئي → `["None"]`. |
| `eligibleCountries` | رموز ISO 3166-1 alpha-2 للدول التي يُقبل منها المتقدّم. **`"XA"`** = كل دول الوطن العربي الـ22. **`"XW"`** = كل العالم. إن قال المصدر "الدول النامية" أو "أفريقيا والشرق الأوسط" فاذكر الدول العربية المشمولة صراحةً، وأضف غيرها إن ذُكرت. هذا الحقل يستبدل `CountryIds` القديم، وأرسلهما معاً خلال فترة التوافق. |
| `hostCountry` | بلد الدراسة أو العمل أو الحدث (ISO-2). للفرص عن بُعد → `null` مع `isRemote: true`. |
| `isRemote` / `isOnline` | `isRemote` للوظائف والتدريب الذي يُؤدَّى من أي مكان. `isOnline` للمؤتمرات والدورات والمسابقات الإلكترونية. |
| `minAge` / `maxAge` | فقط إن ذُكر عمر صريح. "تحت 35" → `maxAge: 35`. |
| `languageRequirement` | `IELTS` أو `TOEFL` أو `Either` إن طُلبت شهادة إنجليزية؛ `French` لشهادة فرنسية؛ `Other` لغير ذلك؛ `None` **فقط** إن قال المصدر صراحةً إن الشهادة غير مطلوبة؛ `Unknown` (أو احذف الحقل) إن لم يذكر المصدر اللغة أصلاً — لا تستنتج `None` من الصمت، لأن صفحة «بدون آيلتس» تعتمد عليها. ضع الدرجة في `languageScore` نصاً كما وردت. |
| `hasApplicationFee` | `true` فقط إن ذُكرت رسوم تقديم أو تسجيل. |
| `forWomenOnly` | `true` إن كانت الفرصة مخصصة للنساء. |
| `startDate` / `venue` | للمؤتمرات والمسابقات والدورات: تاريخ بدء الحدث (ليس الموعد النهائي للتقديم) ومكانه. `EndDate` القديم يبقى **الموعد النهائي للتقديم**. |
| `hostOrganization` | **الجهة التي تقدّم الفرصة فعلاً** (الجامعة، الوزارة، المنظمة، الشركة)، لا الموقع الذي نُشرت فيه ولا "فرص خضراء". `name` إلزامي؛ `url` موقعها الرسمي إن وُجد. |
| `employmentType` | للوظائف والتدريب والتطوع فقط: `FULL_TIME` `PART_TIME` `CONTRACTOR` `TEMPORARY` `INTERN` (للتدريب) `VOLUNTEER` (للتطوع). غير ذلك `null`. |
| `programKey` | مفتاح ثابت صغير بالإنجليزية للبرنامج المتكرر سنوياً، **نفسه كل عام**: `chevening`, `daad-epos`, `erasmus-mundus`, `turkiye-burslari`, `mext`, `fulbright`, `csc`, `mastercard-foundation`, `rcoy-mena` (المفاتيح الثمانية الأولى تربط الفرصة بدليل التقديم في `/guides`). استعمل `GET /api/team/opportunities/search?q=` لترى المفتاح المستعمل في الدورة السابقة. فرصة لمرة واحدة → `null`. |
| `benefitsSummary` | 2–5 عبارات قصيرة لكل لغة (≤ 8 كلمات) تُعرض على البطاقة. |

#### 4. التحقق قبل الإرسال
- `ApplyLink` و`SourceUrl` و`hostOrganization.url` تبدأ بـ`https://` أو `http://` فقط. أي رابط آخر يُرفض.
- `EndDate` بصيغة `YYYY-MM-DD` دائماً (تقويم ميلادي)، وكذلك `startDate`.
- إن كانت الفرصة منتهية في المصدر، لا ترسلها.
- قبل الكتابة استدعِ `GET /api/team/opportunities/search?url=...` لتجنّب التكرار (موجود).
- **إعادة معالجة الفرص القديمة:** لتحديث الحقول المهيكلة لفرصة موجودة (من `SourceUrl`) دون إعادة كتابة نصوصها، أرسل الحقول وحدها إلى `POST /api/team/opportunities/{id}/facets` بالجسم نفسه (كل حقل اختياري؛ ما لا تُرسله لا يتغير). الرد `200` بالفرصة، أو `400` باسم الحقل الخاطئ وقائمة القيم المسموحة (`bad_funding`, `bad_degree`, `bad_field`, `bad_theme`, `bad_country`, `bad_host_country`, `bad_language`, `bad_employment`, `bad_start_date`, `bad_program_key`, `bad_host_url`, `bad_age`).
- **طلبات المنظمات (مفعَّلة منذ 5 نوفمبر 2026):** استدعِ `GET /api/team/requests` دورياً (كل ساعة يكفي)؛ كل طلب فيه `id`, `organizationName`, `contactName`, `contactEmail`, `website`, `sourceUrl`, `notes`, `typeHint`, `countryIso`, `language`. عند أخذه أرسل `POST /api/team/requests/{id}/status` بالجسم `{"status":"inprogress"}`. اكتب الفرصة من `sourceUrl` (و`notes` تلميح من المنظمة لا نص يُنسخ) وأرسلها مع `"requestId": <id>` في جسم `POST opportunities`؛ عند `publish` يُغلق الموقع الطلب ويرسل للمنظمة رابط الفرصة بلغتها تلقائياً. إن لم يكن الرابط فرصة أو كانت منتهية: `{"status":"rejected","note":"السبب بالعربية"}` وتصل المنظمة رسالة اعتذار بالسبب. *(كان النص السابق: حتى حينها ترد 404.)*

#### 5. مثال كامل (منحة)

```json
{
  "titleAr": "منحة DAAD EPOS 2027 للماستر في ألمانيا: ممولة بالكامل لشباب الوطن العربي",
  "titleEn": "DAAD EPOS 2027 Master's Scholarships in Germany: Fully Funded for Youth in the Arab World",
  "titleFr": "Bourses DAAD EPOS 2027 pour le master en Allemagne : entièrement financées pour les jeunes du monde arabe",
  "type": "Scholarships",
  "deadlineType": "specific",
  "endDate": "2026-12-15",
  "applyLink": "https://www.daad.de/go/en/stipa50076777",
  "sourceUrl": "https://www.daad.de/en/study-and-research-in-germany/scholarships/epos/",
  "fundingType": "FullyFunded",
  "degreeLevels": ["Bachelor"],
  "fields": ["Environment", "Engineering", "Economics", "Agriculture"],
  "greenThemes": ["Climate", "Energy", "Water", "Agriculture"],
  "eligibleCountries": ["XA"],
  "hostCountry": "DE",
  "isRemote": false,
  "isOnline": false,
  "minAge": null,
  "maxAge": null,
  "languageRequirement": "Either",
  "languageScore": "IELTS 6.0 أو TOEFL iBT 80",
  "hasApplicationFee": false,
  "forWomenOnly": false,
  "startDate": null,
  "venue": null,
  "hostOrganization": { "name": "DAAD", "url": "https://www.daad.de", "logoUrl": null },
  "employmentType": null,
  "programKey": "daad-epos",
  "benefitsSummary": {
    "ar": ["راتب شهري 934 يورو", "تذكرة طيران", "تأمين صحي", "إعفاء من الرسوم"],
    "en": ["€934 monthly stipend", "Flight ticket", "Health insurance", "Tuition waiver"],
    "fr": ["Allocation de 934 €/mois", "Billet d'avion", "Assurance santé", "Exonération des frais"]
  },
  "descriptionAr": "...", "descriptionEn": "...", "descriptionFr": "...",
  "detailsAr": "...", "detailsEn": "...", "detailsFr": "...",
  "eligibilityCriteriaAr": "<ul><li>بكالوريوس بتقدير جيد جداً</li><li>سنتا خبرة عمل</li></ul>", "eligibilityCriteriaEn": "...", "eligibilityCriteriaFr": "...",
  "benefitsAr": "<ul><li>...</li></ul>", "benefitsEn": "...", "benefitsFr": "...",
  "countryIds": [2],
  "aiGenerated": true,
  "publish": false
}
```

#### 6. مثال (وظيفة عن بُعد)
`"type": "Jobs"`, `"fundingType": "Paid"`, `"degreeLevels": ["Bachelor","Master"]`, `"eligibleCountries": ["EG","JO","LB","MA","TN"]`, `"hostCountry": null`, `"isRemote": true`, `"employmentType": "FULL_TIME"`, `"hostOrganization": {"name": "Climate Action Network", "url": "https://climatenetwork.org"}`, `"programKey": null`.

#### 7. ما يُرفض
- `hostOrganization.name` = "فرص خضراء" أو اسم موقع ناشر.
- `fundingType: "FullyFunded"` مع ذكر رسوم أو تمويل جزئي في النص.
- `eligibleCountries` فارغة.
- عنوان فيه "الشباب العربي" أو "للعرب".
