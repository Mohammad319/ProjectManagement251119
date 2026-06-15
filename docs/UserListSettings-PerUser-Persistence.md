# حفظ إعدادات قوائم المشاريع/الحسابات لكل مستخدم على السيرفر

**الفرع:** `b19` · **التاريخ:** 2026-06-15

## الهدف
نقل إعدادات عرض قائمتَي المشاريع (`ProjectsUI`) والحسابات (`ProjectsCalculationList`) من
`localStorage` فقط (مرتبطة بالجهاز) إلى **حفظ دائم على السيرفر لكل مستخدم/tenant**، مع إبقاء
المتصفح كـ **cache** لتحسين السرعة و(احتياط offline). السيرفر هو المصدر الموثوق.

الإعدادات المشمولة (٤ أنواع):
- **VisibleColumns** — الأعمدة المرئية (Select columns)
- **ColumnWidths** — عرض الأعمدة (السحب/resize) — *لم يكن يُحفظ إطلاقًا من قبل*
- **SavedFilters** — الفلاتر المحفوظة (القائمة المنسدلة)
- **SavedColumnViews** — عروض الأعمدة المحفوظة (Kolumnvyer)

## نموذج التخزين
جدول واحد عام `UserListSettings`، صف لكل `(TenantId, UserId, Scope, Kind)` مع حمولة JSON:
- **Scope:** `ProjectList` | `CalculationList`
- **Kind:** `VisibleColumns` | `ColumnWidths` | `SavedFilters` | `SavedColumnViews`
- **Payload:** نفس شكل الـ JSON الذي كان يُخزَّن في localStorage (توافق كامل للبيانات الحالية)
- فهرس فريد: `UX_UserListSettings_Tenant_User_Scope_Kind`

`TenantId` يُضبط تلقائيًا عبر `TenantAuditSaveChangesInterceptor`، والقراءة تُفلتر تلقائيًا
بالفلتر العام في `ShardingSingleDbContext`. الـ controller يمرّر `UserId` صراحةً (والـ tenant ambient).

## الملفات

### جديدة (Backend)
- `Domain/Entities/Users/UserListSettingEntity.cs`
- `Persistence/Configurations/UserListSettingConfiguration.cs`
- `Persistence/Service/UserSettings/UserListSettingService.cs`
- `Application/Feature/General/ListSettings/IUserListSettingService.cs`
- `Application/Feature/General/ListSettings/Queries/GetUserListSettingsQuery.cs`
- `Application/Feature/General/ListSettings/Commands/UpsertUserListSettingCommand.cs`
- `ProjectManagement.Shared/DTO/UserSettings/UserListSettingDTO.cs`
- `ProjectManagement/ProjectManagement/Controllers/v1/UserSettings/UserListSettingsController.cs`
  (`GET ?scope=` و `PUT`)
- `Persistence/Migrations/20260615074440_AddUserListSettings*.cs`

### جديدة (Client)
- `ProjectManagement.Client.Shared/Repositories/UserSettings/UserListSettingsRepository.cs`

### معدّلة (Backend)
- `Persistence/Context/ShardingSingleDbContextDataset.cs` — إضافة `DbSet<UserListSettingEntity>`
- `Persistence/Factory/PersistenceContainer.cs` — تسجيل `IUserListSettingService`
- `Persistence/Migrations/ShardingSingleDbContextModelSnapshot.cs`

### معدّلة (Client)
- `ProjectManagement.Client.Shared/Constants/UrlsAPI.cs` — مسار `UserListSettings`
- `ProjectManagement/ProjectManagement.Client/DependencyInjection/RepositoriesCollection.cs` — تسجيل الـ repository
- `Services/Folder/ListSavedFilterStorage.cs` — DB-backed + cache + هجرة لمرة واحدة
- `Services/Folder/ListSavedColumnViewStorage.cs` — نفس النمط
- `Services/Folder/ProjectListViewPreference.cs` — أعمدة مرئية + عرض الأعمدة
- `Services/Calculation/CalculationListViewPreference.cs` — نفس النمط
- `Pages/Project/ProjectPages/ProjectsUI.razor` — ربط `SaveTemplateBlazor` + تحميل/استعادة العرض
- `Pages/Project/ProjectPages/ProjectsCalculationList.razor` — نفس الربط
- `ProjectManagement/ProjectManagement/wwwroot/js/resizableTable.js` — دالة `applySavedColumnWidths` (إضافية)

## السلوك
- **التحميل:** قراءة الـ cache فورًا (عرض سريع) ← جلب من السيرفر ← تحديث الواجهة والـ cache (السيرفر يحكم).
- **الحفظ:** write-through (تحديث الـ cache + PUT للسيرفر).
- **الهجرة:** عند أول تحميل إذا كان السيرفر فارغًا والـ cache يحوي بيانات، تُرفع بيانات الـ cache
  للسيرفر مرة واحدة (حتى لا يفقد المستخدمون فلاترهم/أعمدتهم الحالية).
- **عرض الأعمدة:** `resizableTable.js` يرسل `key||width` ← `SaveTemplateBlazor` يحلّله ←
  `SaveColumnWidthsAsync`؛ وعند التهيئة `applySavedColumnWidths` يعيد العرض.

## ملاحظة معمارية مهمة
`resizableTable.js` يستخدم مرجعًا عامًّا واحدًا (`window.nelCalcDotNetRef`) مشتركًا مع `CalcDataGrid`.
في وضع "Kalkyler" داخل `ProjectsUI` يُعرَض `ProjectsCalculationList` كابن، لذا تمّ منع `ProjectsUI`
من امتلاك المرجع إلا في وضع المشاريع (`_contentMode == ContentModeProjects`) لتجنّب توجيه تغييرات
عرض أعمدة الحسابات إلى النطاق الخطأ.

> `CalcDataGrid` لم يُعدَّل إطلاقًا — حفظ عرض أعمدته يبقى على الـ Template المشترك (وليس لكل مستخدم).

## قاعدة البيانات
الـ migration `AddUserListSettings` طُبّق على `PM_Tenant_DB1` و`PM_Tenant_DB2`
(`dotnet ef database update` مع متغيّر البيئة `PM_TEMPLATE_CONN` لكل قاعدة). القواعد كانت عند
الـ migration السابق مباشرةً، فلم يُطبَّق سوى هذا.

أوامر إعادة التطبيق عند الحاجة (لكل قاعدة tenant):
```bash
PM_TEMPLATE_CONN="Data Source=.\\SQLEXPRESS;Initial Catalog=PM_Tenant_DB1;User ID=...;Password=...;TrustServerCertificate=True" \
dotnet ef database update --project Persistence --startup-project ProjectManagement/ProjectManagement
```

## نتيجة التحقق (PASS)
- **API مصادَق (cookie):** PUT يحفظ، GET يعيد الحمولة بدقة؛ upsert يحدّث دون تكرار؛ عزل النطاق؛
  عزل لكل مستخدم (مستخدم آخر يرى قائمة فارغة)؛ 400 على مدخل ناقص.
- **متصفح حقيقي (Playwright/Chrome):** سحب فاصل عمود "Projektstatus" (174→264) ← ظهر `PUT` فعليًا ←
  بعد إعادة التحميل بقي 264. القاعدة: `ColumnWidths = {"code":999,"status":264}`.
- البناء الكامل للحل نظيف (لا أخطاء).
- (٣ اختبارات `CalculationComponentRenderTests` فاشلة مسبقًا على نسخة نظيفة — غير متعلّقة بهذا العمل.)

## ملاحظة
الإعدادات شخصية بالكامل لكل مستخدم حاليًا (لا مشاركة). يمكن لاحقًا إضافة حقل `IsShared`
لمشاركة فلاتر/عروض على مستوى القسم أو الـ tenant دون تغيير النموذج جوهريًا.
