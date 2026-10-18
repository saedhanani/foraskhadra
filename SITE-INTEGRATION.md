# ربط foraskhadra.com بمنصة الفريق (team.foraskhadra.com)

## ما تغيّر في هذا المستودع
| ملف | التغيير |
|---|---|
| `Models/OpportunityStatus.cs` | جديد: Draft / Published |
| `Models/Opportunity.cs` | أعمدة: `Status` (افتراضي Published)، `SourceUrl`، `PipeId`، `AiGenerated` |
| `Controllers/Api/TeamApiController.cs` | جديد: واجهة المنصة بمفتاح `X-Team-Key` |
| `Controllers/HomeController.cs`, `OpportunityController.cs` | الصفحات العامة تعرض المنشور فقط؛ المسودة تظهر لحساب Admin فقط |
| `Views/Opportunity/Index.cshtml` | وسم «مسودة من منصة الفريق» في قائمة الإدارة |
| `Migrations/20261018000000_TeamPipeline.cs` + `Migrations/Sql/2026-10-team-pipeline.sql` | الهجرة (كود أو SQL) |
| `Program.cs` | `app.MapControllers()`؛ إزالة طباعة سلسلة الاتصال؛ زرع الأدمن من الإعدادات فقط؛ إزالة `UseDeveloperExceptionPage` من الإنتاج |
| `appsettings.json` / `appsettings.example.json` | الأسرار أُزيلت (CHANGE_ME)؛ قسما `TeamApi` و`Seed` |
| `.gitignore` | استثناء ملفات الإعدادات المحلية |

## خطوات النشر (بالترتيب)
1. **الأسرار:** غيّر كلمة مرور قاعدة البيانات في site4now وكلمة مرور Gmail (كانتا في المستودع). ضعهما في
   `appsettings.Production.json` على الخادم فقط (أو متغيرات البيئة في لوحة SmarterASP)، لا في المستودع.
2. **المفتاح:** ولّد 40 حرفًا عشوائيًا وضعه في `TeamApi:Key`، وضع بريد حساب أدمن حقيقي في `TeamApi:UserEmail`
   (هو الذي تُسجَّل باسمه المسودات). نفس المفتاح يُدخل في المنصة: مسار الفرص > الإعدادات > الربط بالموقع.
3. **قاعدة البيانات:** إما `dotnet ef database update` من جهازك (الهجرة مضافة، واللقطة محدّثة)، أو نفّذ
   `Migrations/Sql/2026-10-team-pipeline.sql` مباشرة في SQL Server ثم أضف سطر `__EFMigrationsHistory`
   الموجود في نهاية السكربت.
4. **النشر:** Publish كالمعتاد. ثم تحقق: `https://www.foraskhadra.com/api/team/ping` مع الترويسة `X-Team-Key`
   (دون الترويسة يرد 401).
5. في المنصة اضغط «اختبار الاتصال».

## الواجهة (كلها تحت `/api/team`، ترويسة `X-Team-Key` إلزامية)
| الطريقة | المسار | الغرض |
|---|---|---|
| GET | `ping` | تحقق من الربط |
| GET | `countries` | قائمة الدول `{id, nameAr, nameEn}` |
| GET | `opportunities/search?url=&q=` | كشف التكرار بالرابط (بعد تطبيعه) أو بكلمات العنوان |
| GET | `opportunities/examples?n=8&type=` | أمثلة أسلوب من المنشور (لتغذية الكتابة الآلية) |
| GET | `opportunities/{id}` | حالة فرصة |
| POST | `opportunities` | إنشاء **مسودة** بكل الحقول (15 نصًا × 3 لغات، النوع، الموعد، الدول، الرابط) |
| POST | `opportunities/{id}/publish` | نشر المسودة؛ يرد `no_image` إن لم تُرفع الصورة، و`expired` إن مضى الموعد |
| POST | `opportunities/{id}/discard` | حذف مسودة لم تُنشر |

المسودات لا تظهر للزوار، ولا في الصفحة الرئيسية ولا في صفحات الأنواع ولا البحث؛ تظهر في `Opportunity/Index`
لحساب Admin بوسم، وتُعدَّل من `Opportunity/Edit/{id}` كأي فرصة (هناك يرفع المدقق الصورة).
