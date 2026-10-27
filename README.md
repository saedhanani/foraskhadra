# فرص خضراء — Foras Khadra

منصة عربية لنشر الفرص (وظائف، منح، زمالات، تدريب، تطوع، مؤتمرات، مسابقات) بثلاث لغات، مع حسابات للمنظمات ولوحة إدارة، وواجهة ربط مع منصة الفريق الداخلية.

- **التقنية:** ASP.NET Core 8 MVC · Entity Framework Core 8 · SQL Server · ASP.NET Identity
- **الموقع:** https://www.foraskhadra.com
- **ربط منصة الفريق:** انظر [SITE-INTEGRATION.md](SITE-INTEGRATION.md)

## التشغيل محليًا

1. ثبّت .NET SDK 8 و SQL Server LocalDB (يأتي مع Visual Studio).
2. انسخ `appsettings.example.json` إلى `appsettings.Development.json` وعدّل:
   - `ConnectionStrings:DefaultConnection` (الافتراضي المحلي: `Server=(localdb)\MSSQLLocalDB;Database=ForasKhadra_Final;Trusted_Connection=True;TrustServerCertificate=True;`)
   - `Seed:AdminEmail` و`Seed:AdminPassword` لإنشاء أول حساب أدمن عند التشغيل (كلمة المرور: 8 أحرف على الأقل، كبير وصغير ورقم ورمز).
   - `TeamApi:Key` (40 حرفًا عشوائيًا) إن أردت تجربة واجهة المنصة.
3. أنشئ قاعدة البيانات:
   ```bash
   dotnet ef database update
   ```
4. شغّل الموقع:
   ```bash
   dotnet run --launch-profile http
   ```
   ثم افتح http://localhost:5000. في بيئة Development تُزرع بيانات تجريبية (مقالات، منظمات، فرص) تلقائيًا.

ملفات `appsettings.json` و`appsettings.*.json` مستثناة من git عمدًا؛ لا تضع الأسرار في المستودع.

## الاختبارات

```bash
dotnet test
```

مشروع `Foras_Khadra.Tests` (xUnit) يغطي تعقيم HTML، التحقق من الصور المرفوعة، وتطبيع الروابط في واجهة المنصة.

## البنية

| المجلد | المحتوى |
|---|---|
| `Controllers/` | MVC: الصفحات العامة (Home)، الحسابات (Account/Organization)، الإدارة (Admin/Articles/Team/Opportunity) |
| `Controllers/Api/` | `TeamApiController` — واجهة منصة الفريق بمفتاح `X-Team-Key` |
| `Models/` | الكيانات و ViewModels |
| `Data/` | `ApplicationDbContext` و`DbInitializer` (بيانات التجربة) |
| `Services/` | البريد (`MailService`, `EmailSender`) ورفع الصور (`ImageUploadService`) |
| `Helpers/` | `SafeHtml` (تعقيم HTML) ومساعدات الترجمة |
| `Resources/` | ملفات الترجمة (.resx) للعربية والإنجليزية والفرنسية |
| `Migrations/` | هجرات EF Core + سكربت SQL يدوي في `Migrations/Sql/` |
| `wwwroot/` | الملفات الثابتة. `uploads/` و`logos/` رفوعات المستخدمين — **خارج git**، تبقى على الخادم |

## الأمان والتشغيل

- كل POST يتحقق من رمز CSRF تلقائيًا (واجهة المنصة مستثناة لأنها بالمفتاح).
- الرفع مقصور على JPG/PNG/WEBP/GIF حتى 5MB مع فحص محتوى الملف.
- HTML القادم من المحررات يُعقَّم عند الحفظ وعند العرض.
- قفل الحساب 15 دقيقة بعد 5 محاولات دخول فاشلة، وحدود للطلبات على الدخول ونموذج التواصل وواجهة المنصة.
- السجلات: Serilog إلى الكونسول وملف يومي في `logs/` (30 يومًا).
- فحص الصحة: `GET /health` يعيد `Healthy` عند توفر قاعدة البيانات — اربطه بأداة مراقبة خارجية.

## النشر

Publish كالمعتاد إلى الاستضافة. ضع الأسرار (سلسلة الاتصال، بريد Gmail، `TeamApi:Key`، `Seed`) في `appsettings.Production.json` على الخادم أو في متغيرات البيئة، لا في المستودع. مجلدا `wwwroot/uploads` و`wwwroot/logos` على الخادم هما المصدر الوحيد للصور المرفوعة — لا تفعّل خيار "حذف الملفات الإضافية" عند النشر.
