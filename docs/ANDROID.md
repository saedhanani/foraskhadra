# تطبيق Android على Google Play (المرحلة 4.7 — بقرار المالك، 25$ مرة واحدة)

الموقع تطبيق ويب تقدّمي (PWA) جاهز: `manifest.webmanifest`، `sw.js`، أيقونات maskable، صفحة offline. تغليفه كتطبيق Android يتم بـ**Trusted Web Activity** عبر PWABuilder بلا كتابة كود، ويبقى التحديث من الموقع نفسه.

## الخطوات

1. **حساب مطوّر Google Play** (25$ مرة واحدة): https://play.google.com/console — باسم المنظمة.
2. **PWABuilder:** https://www.pwabuilder.com → أدخل `https://www.foraskhadra.com` → **Package for stores → Android**.
   - Package ID: `com.foraskhadra.app` · App name: فرص خضراء · Short name: فرص خضراء · Theme: `#1B6B23`.
   - Signing key: اختر **Generate new** واحفظ ملف `.keystore` وكلمتَيه في مكان آمن (لا يمكن استرجاعهما؛ فقدانهما يعني تطبيقًا جديدًا).
   - حمّل الحزمة: `.aab` للرفع، و`assetlinks.json` ملف المرجع، وملف التوقيع.
3. **ربط التطبيق بالموقع (حتى يفتح بلا شريط عنوان):** من ملف `assetlinks.json` خذ بصمة SHA-256 وأضفها إلى إعدادات الخادم (`appsettings.production.json`):
   ```json
   "Android": {
     "PackageName": "com.foraskhadra.app",
     "Sha256Fingerprints": [ "AA:BB:CC:…" ]
   }
   ```
   الموقع يخدمها آليًا من `https://www.foraskhadra.com/.well-known/assetlinks.json` (`Controllers/WellKnownController.cs`). بعد النشر على Play يجب إضافة بصمة **مفتاح توقيع Play** أيضًا (Play Console → Setup → App signing → SHA-256) إلى القائمة نفسها، وإلا يظهر شريط العنوان.
4. **Play Console:** أنشئ التطبيق → ارفع `.aab` في Internal testing أولًا → أكمل: سياسة الخصوصية (`/Home/PolicyPrivacy`)، تصنيف المحتوى، الجمهور (13+)، لقطات شاشة (الهاتف 1080×1920، شعار 512×512، صورة مميزة 1024×500) → أرسل للمراجعة (عادة 1–7 أيام).
5. **بعد النشر:** أضف رابط المتجر في `Views/Home/App.cshtml` وزر «حمّل من Google Play» في التذييل، وفعّل `source=play` في الروابط لتظهر في التحليلات كمصدر.

## ملاحظات

- التطبيق يعرض الموقع نفسه؛ أي تحديث على الموقع يصل فورًا بلا إصدار جديد. يُعاد الرفع فقط لتغيير الأيقونة أو الاسم أو الإصدار الأدنى.
- الإشعارات داخل التطبيق هي إشعارات الدفع الحالية (Web Push) وتعمل داخل TWA على Android.
- iOS: يبقى التثبيت من Safari («إضافة إلى الشاشة الرئيسية») كما في `/Home/App`.
