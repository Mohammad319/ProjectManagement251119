# نظام TF-IDF للبحث الذكي في المهام

## المشكلة التي يحلها

عند البحث عن مهمة باسم **"Schakt för planteringsyta, arbetsområde A"**:

- البحث بـ `"Schakt för planteringsyta"` → يجد النتيجة (45%)
- البحث بـ `"Schakt planteringsyta"` (بدون "för") → **لا يجد شيئاً**

السبب: السيرفر يبحث بالمطابقة الحرفية (Contains)، فـ "Schakt planteringsyta" ليست موجودة كسلسلة نصية متصلة في اسم المهمة.

الحل: بدلاً من إرسال الجملة كاملة للسيرفر، نختار **الكلمة الأكثر تميزاً** فيها ونرسلها وحدها، ثم يقوم البحث الضبابي (Fuzzy Search) بترتيب النتائج على جانب العميل.

---

## ما هو TF-IDF

**TF-IDF** اختصار لـ Term Frequency – Inverse Document Frequency.

الفكرة الأساسية:
- كلمة تظهر في **كل المهام** → غير مميزة → وزن منخفض
- كلمة تظهر في **مهام قليلة** → مميزة جداً → وزن عالٍ

المعادلة المستخدمة (Sklearn Smoothed IDF):

```
IDF(كلمة) = log( (N + 1) / (df + 1) ) + 1
```

| المتغير | المعنى |
|---------|--------|
| N  | عدد المهام الكلي في قاعدة البيانات |
| df | عدد المهام التي تحتوي على هذه الكلمة |

مثال عملي:

| الكلمة | تكررت في | الوزن |
|--------|---------|-------|
| `schakt` | 200 من 500 مهمة | منخفض |
| `planteringsyta` | 3 من 500 مهمة | عالٍ |
| `va100` | 1 من 500 مهمة | عالٍ جداً |

---

## البنية المعمارية

```
┌─────────────────────────────────────────────────────────────┐
│  المتصفح (WASM)                                             │
│                                                             │
│  TaskList.razor                                             │
│    │                                                        │
│    ├── عند التحميل: يجلب TfIdf Index من السيرفر مرة واحدة │
│    │                                                        │
│    └── عند البحث:                                          │
│         BestServerToken("Schakt planteringsyta")            │
│           → Normalize → ["schakt", "planteringsyta"]        │
│           → TfIdfIndex.BestToken() → "planteringsyta"       │
│           → filterDto.NameOrCode = "planteringsyta"         │
│           → السيرفر يجد المهمة ✓                           │
│           → Fuzzy Score يرتب النتائج ✓                      │
└─────────────────────────────────────────────────────────────┘
              ↑ GET /api/v1/Storages/tfidf (مرة واحدة)
┌─────────────────────────────────────────────────────────────┐
│  السيرفر (ASP.NET Core)                                     │
│                                                             │
│  StoragesController → GET tfidf                             │
│    │                                                        │
│    └── TfIdfIndexService                                    │
│         ├── يقرأ NormalizedTextSv من Blueprint DB (عامة)   │
│         ├── يقرأ NormalizedTextSv من Tenant DB (خاصة)      │
│         ├── يحسب IDF لكل كلمة                              │
│         └── يخزن النتيجة في IMemoryCache لمدة ساعتين       │
└─────────────────────────────────────────────────────────────┘
```

---

## الملفات وأدوارها

### مشترك (Shared)
| الملف | الدور |
|-------|-------|
| `ProjectManagement.Shared/Helper/Text/TfIdfIndex.cs` | هيكل البيانات النقي — يخزن أوزان IDF ويحسب أفضل كلمة |
| `ProjectManagement.Shared/Helper/Text/SwedishTaskTextNormalizer.cs` | تحويل النص إلى رموز مُعيَّرة (Stop Words، Stemming، إلخ) |
| `ProjectManagement.Shared/Helper/Text/FuzzySearchHelper.cs` | حساب درجة التشابه الضبابي بين النص المُعيَّر |

### السيرفر
| الملف | الدور |
|-------|-------|
| `Application/Feature/TfIdf/ITfIdfIndexProvider.cs` | واجهة الخدمة |
| `Persistence/Service/TfIdf/TfIdfIndexService.cs` | بناء مؤشر IDF من قاعدتَي البيانات وتخزينه مؤقتاً |
| `Controllers/v1/CalcExtension/StoragesController.cs` | نقطة الـ API: `GET /api/v1/Storages/tfidf` |
| `Persistence/Factory/PersistenceContainer.cs` | تسجيل الخدمة في نظام الـ DI |

### العميل (WASM)
| الملف | الدور |
|-------|-------|
| `ProjectManagement.Client.Shared/Services/TfIdf/TfIdfClientService.cs` | تخزين مؤقت للـ Index في جلسة المتصفح |
| `ProjectManagement.Client.Shared/Repositories/Calculation/IStorageRepository.cs` | إضافة `GetTfIdfAsync()` لواجهة المستودع |
| `ProjectManagement.Client.Shared/Repositories/Calculation/Implement/StorageRepository.cs` | تنفيذ استدعاء HTTP لجلب الـ Index |
| `DependencyInjection/RepositoriesCollection.cs` | تسجيل `TfIdfClientService` في DI |
| `Pages/Project/Storage/App/TaskList.razor` | استخدام الـ Index في `BestServerToken()` |

---

## سير العمل خطوة بخطوة

### عند فتح صفحة المهام

1. `OnInitializedAsync` يشغّل **بالتوازي**:
   - جلب قائمة المهام (`Refresh()`)
   - جلب مؤشر TF-IDF (`LoadTfIdfAsync()`)
2. `LoadTfIdfAsync` يتحقق: هل الـ Index محمّل مسبقاً؟
   - نعم → لا يفعل شيئاً (الـ Index يعيش طول جلسة المتصفح)
   - لا → يطلبه من السيرفر ويخزنه في `TfIdfClientService`

### عند الكتابة في خانة البحث

```
المستخدم يكتب: "Schakt planteringsyta"
                        ↓
OnSearchInput()
                        ↓
_rawSearchQuery = "Schakt planteringsyta"  ← يُعرض في الـ input
                        ↓
BestServerToken("Schakt planteringsyta")
  → SwedishTaskTextNormalizer.Normalize()
  → ["schakt", "planteringsyta"]
  → TfIdfIndex.BestToken(["schakt", "planteringsyta"])
  → يقارن IDF("schakt") vs IDF("planteringsyta")
  → "planteringsyta" أعلى وزناً (أندر في الكوربس)
  → يختارها
                        ↓
filterDto.NameOrCode = "planteringsyta"  ← يُرسل للسيرفر
                        ↓
السيرفر يعيد: ["Schakt för planteringsyta, arbetsområde A", ...]
                        ↓
GetScoredGroups()
  → FuzzySearchHelper.Score("Schakt planteringsyta", "Schakt för planteringsyta, arbetsområde A")
  → درجة عالية ✓ (تظهر للمستخدم)
```

---

## التخزين المؤقت (Caching)

| المستوى | المدة | التفاصيل |
|---------|-------|---------|
| السيرفر — IMemoryCache | ساعتان | مفتاح: `"tfidf_{tenantId}"` — يُعاد بناؤه عند انتهاء المدة |
| المتصفح — TfIdfClientService | جلسة المتصفح | يُجلب مرة واحدة عند أول فتح للصفحة |

---

## مصادر البيانات

يُبنى الـ Index من مصدرين:

1. **Blueprint DB** (`TaskResourceBlueprintsContext`) — المهام العامة التي يضيفها مدير النظام، مشتركة بين جميع العملاء.
2. **Tenant DB** (`ShardingSingleDbContext`) — مهام العميل الخاصة، مفلترة بـ `TenantId` تلقائياً.

الحقل المستخدم: `NormalizedTextSv` — نص مُعيَّر مُخزَّن مسبقاً على كل مهمة ويحتوي على الرموز النقية بعد إزالة Stop Words والتطبيع.

---

## خطأ محتمل والتعامل معه

إذا فشل جلب الـ Index (انقطاع الشبكة، خطأ في الـ API، إلخ):
- `LoadTfIdfAsync` يبتلع الاستثناء بصمت
- `TfIdfClientService.Index` يعيد `TfIdfIndex.Empty`
- `BestServerToken` تتراجع لـ `FallbackBestToken()`:
  - تُفضّل الكلمات التي تحتوي أرقاماً (رموز المواد كـ VA-100)
  - ثم أطول كلمة

النتيجة: البحث يعمل — بجودة أقل قليلاً — دون أي رسالة خطأ للمستخدم.
