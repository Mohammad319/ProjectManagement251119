# إعداد Environment Variables لتطبيقات IIS

هذا الملف يوضح طريقة تخزين القيم المهمة خارج ملفات النشر لكل من:

| التطبيق | Application Pool | مسار النشر |
|---|---|---|
| الموقع الرئيسي | `PM_Tenant` | `C:\inetpub\PM\Tenant` |
| لوحة الإدارة | `PM_Admin` | `C:\inetpub\PM\WebAdmin` |

استخدم Application Pool منفصلاً لكل تطبيق حتى لا يحدث تعارض بين التطبيقات عند استخدام أسماء متغيرات متشابهة.

## قبل البدء

افتح PowerShell بصلاحية Administrator:

```powershell
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
```

استبدل القيم التي تبدأ بـ `REPLACE_` بالقيم الفعلية عند التنفيذ. لا تحفظ كلمات المرور الحقيقية داخل هذا الملف أو داخل Git.

## PM_Tenant

```powershell
& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='ConnectionStrings__AuthPermissionsConnection',value='REPLACE_MAIN_CONNECTION']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='ConnectionStrings__BlueprintsConnection',value='REPLACE_BLUEPRINTS_CONNECTION']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='DataProtection__KeysPath',value='C:\inetpub\PM\DataProtectionKeys']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='TenantReload__Secret',value='REPLACE_RANDOM_SECRET']" `
  /commit:apphost
```

## PM_Admin

```powershell
& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Admin'].environmentVariables.[name='ConnectionStrings__AuthPermissionsConnection',value='REPLACE_MAIN_CONNECTION']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Admin'].environmentVariables.[name='ConnectionStrings__BlueprintsConnection',value='REPLACE_BLUEPRINTS_CONNECTION']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Admin'].environmentVariables.[name='DataProtection__KeysPath',value='C:\inetpub\PM\AdminDataProtectionKeys']" `
  /commit:apphost
```

## متغيرات اختيارية

أضف هذه القيم فقط للتطبيق الذي يستخدمها:

```text
MailSettings__Password
Sentry__Dsn
PriceImportAi__ApiKey
```

مثال:

```powershell
& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='MailSettings__Password',value='REPLACE_MAIL_PASSWORD']" `
  /commit:apphost
```

## تطبيق التغييرات

```powershell
Import-Module WebAdministration
Restart-WebAppPool PM_Tenant
Restart-WebAppPool PM_Admin
```

## تعديل قيمة موجودة

احذف القيمة القديمة ثم أضف الجديدة:

```powershell
& $appcmd set config -section:system.applicationHost/applicationPools `
  "/-[name='PM_Tenant'].environmentVariables.[name='TenantReload__Secret']" `
  /commit:apphost

& $appcmd set config -section:system.applicationHost/applicationPools `
  "/+[name='PM_Tenant'].environmentVariables.[name='TenantReload__Secret',value='REPLACE_NEW_RANDOM_SECRET']" `
  /commit:apphost
```

## تنظيف ملفات النشر

بعد ضبط Environment Variables:

1. احذف كلمات المرور ومفاتيح API من `appsettings.Production.json`.
2. لا تضع الأسرار داخل `web.config`.
3. اترك `ASPNETCORE_ENVIRONMENT=Production` داخل `web.config`.
4. لا تحفظ سكربت PowerShell يحتوي على كلمات المرور الحقيقية.

## ملاحظات مهمة

- `ConnectionStrings__AuthPermissionsConnection` يطابق `ConnectionStrings:AuthPermissionsConnection`.
- `ConnectionStrings__BlueprintsConnection` يطابق `ConnectionStrings:BlueprintsConnection`.
- `DataProtection__KeysPath` يطابق `DataProtection:KeysPath`.
- Environment Variables تتغلب على القيم الموجودة في `appsettings.json` و`appsettings.Production.json`.
- تخزين الأسرار في Application Pool يمنع نشرها مع ملفات التطبيق، لكنه ليس تشفيراً كاملاً.
- إذا كان SQL Server على نفس السيرفر، الأفضل استخدام Windows Authentication لتجنب تخزين كلمة مرور قاعدة البيانات.
