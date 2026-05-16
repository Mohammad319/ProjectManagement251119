# محرك البحث الذكي — ملخص ما تم بناؤه وما يمكن تحسينه

---

## ما تم بناؤه ✅

### المرحلة 1 — البنية الأساسية

| الميزة | الوصف | الملفات |
|--------|-------|---------|
| **NormalizedTextSv** | حقل مُخزَّن على كل مهمة يحتوي نصاً مُعيَّراً (بدون Stop Words، مع Stemming) | `TaskDefinition.cs`, `TaskEntity.cs` |
| **SwedishTaskTextNormalizer** | خط معالجة نصي: PhraseMap → Tokenize → StopWords → TokenMap → Stemming → Synonyms → CompoundSplit | `ProjectManagement.Shared/Helper/Text/SwedishTaskTextNormalizer.cs` |
| **FuzzySearchHelper** | حساب درجة التشابه بين نصين مُعيَّرين (Weighted Jaccard) | `ProjectManagement.Shared/Helper/Text/FuzzySearchHelper.cs` |

---

### المرحلة 2 — TF-IDF

| الميزة | الوصف | الملفات |
|--------|-------|---------|
| **TfIdfIndex** | هيكل بيانات نقي — يخزن أوزان IDF ويختار أفضل كلمة | `ProjectManagement.Shared/Helper/Text/TfIdfIndex.cs` |
| **TfIdfIndexService** | يبني مؤشر IDF من Blueprint DB + Tenant DB، مخزَّن 30 دقيقة | `Persistence/Service/TfIdf/TfIdfIndexService.cs` |
| **ITfIdfIndexProvider** | واجهة الخدمة مع `GetIdfScoresAsync` + `InvalidateAll` | `Application/Feature/TfIdf/ITfIdfIndexProvider.cs` |
| **TfIdfClientService** | تخزين مؤقت للـ Index في جلسة المتصفح | `ProjectManagement.Client.Shared/Services/TfIdf/TfIdfClientService.cs` |
| **GET /api/v1/Storages/tfidf** | نقطة API لجلب الـ IDF scores | `StoragesController.cs` |

---

### المرحلة 3 — OR Multi-token Search

| الميزة | الوصف |
|--------|-------|
| **SearchTokens** | الـ Client يرسل أفضل 2 كلمات (بأعلى IDF) بدلاً من جملة كاملة |
| **OR-Contains على السيرفر** | السيرفر يبحث بـ OR على `NormalizedTextSv`, `Name`, `Code` لكل token |
| **ProjectTaskFilterDto** | يحتوي `SearchTokens` + `NameOrCode` (fallback) |

---

### المرحلة 4 — تحسينات لغوية

| الميزة | الوصف |
|--------|-------|
| **تقطيع الكلمات المركبة** | `vattenledning` → `vatten` + `ledning` (يُضاف كلاهما للـ NormalizedTextSv) |
| **تحمل الأخطاء الإملائية** | Damerau-Levenshtein: `betnog` ↔ `betong` (مسافة = 1 = تطابق فازي) |
| **مرادفات سويدية-سويدية** | `regnvatten` ↔ `dagvatten`، `schaktning` ↔ `grävning`، ~40 زوج |

---

### المرحلة 5 — التحسينات الأخيرة

| الميزة | الوصف | الملف |
|--------|-------|-------|
| **Both-tokens Bonus** | +0.15 في الدرجة عند وجود كلا الـ tokens في النتيجة | `TaskList.razor` |
| **Short-query Prefix Fallback** | استعلامات ≤ 3 أحرف تُرسَل كـ raw بدون normalization | `TaskList.razor` |
| **All-stopwords Fallback** | إذا أزال المُعيِّر كل الكلمات، يُرسَل الاستعلام الخام | `TaskList.razor` |
| **Cache Invalidation** | `InvalidateAll()` يُفرغ الـ IDF cache لجميع المستأجرين | `TfIdfIndexService.cs` |
| **Auto-invalidate بعد Rebuild** | `POST /rebuild-normalized-text` يُفرغ الـ cache تلقائياً | `StoragesController.cs` |
| **إعادة بناء NormalizedTextSv** | `POST /rebuild-normalized-text` يُعيد حساب الحقل لجميع المهام القديمة | `StoragesController.cs` |

---

## كيف يعمل النظام الآن

```
المستخدم يكتب: "schakt planteringsyta"
        ↓
BuildServerFilter()
  → Normalize → ["schakt", "planteringsyta"]
  → TF-IDF → "planteringsyta" أعلى وزناً
  → SearchTokens = ["planteringsyta", "schakt"]
        ↓
السيرفر — OR-Contains على NormalizedTextSv + Name + Code
  → يجد: "Schakt för planteringsyta, arbetsområde A"
        ↓
GetScoredGroups() — Client-side
  → FuzzySearchHelper.Score() → 0.80
  → Both-tokens Bonus → كلا الـ tokens موجودان → +0.15
  → الدرجة النهائية: 0.95
        ↓
النتيجة تظهر للمستخدم مرتبة بدرجة 95%
```

---

## نقاط القوة الحالية

- **بحث جزئي**: "schak" يجد "Schaktning..." (via Contains)
- **بحث بدون stop words**: "schakt planteringsyta" يجد "Schakt *för* planteringsyta"
- **بحث بأخطاء إملائية**: "betnog" يجد "betong"
- **بحث بمرادفات**: "regnvatten" يجد "dagvatten"
- **بحث بأجزاء كلمات مركبة**: "ledning" يجد "vattenledning"
- **بحث بكودات**: "VA" يجد "VA100", "VA-200"
- **الترتيب ذكي**: النتائج الأقوى تطابقاً تظهر أولاً

---

## ما تم بناؤه إضافياً ✅

### Usage Boost — الترتيب بالاستخدام
- عمود `UsageCount` أُضيف لـ `TaskDefinition` **(يحتاج migration)**
- الترتيب في `GetTasksForUserDtoAsync` أصبح `UsageCount DESC` → `Code` → `Name`
- يزداد `UsageCount` عند كل جلب مفصَّل لمهمة عبر `GET /tasksapp2/{id}`

### Bigram Indexing — فهرسة أزواج الكلمات
- `Normalize()` يُولِّد bigrams بصيغة `schakt_planteringsyta` + `planteringsyta_schakt`
- مخزَّنة في NormalizedTextSv بعد الكلمات الفردية (تُقطع أولاً عند الحد)
- `BuildServerFilter` يُرسل البيغرام كـ token ثالث في `SearchTokens`
- السيرفر يبحث عن البيغرام في NormalizedTextSv كـ OR إضافي

### توسيع القواميس
- `CompoundRoots`: +15 جذر (fjärrvärme، fiber، spont، trapp، parkering...)
- `SynonymMap`: +30 مرادف (asfaltering، gatuarbete، LOD، stormwater، elarbete...)
- `TokenMap`: +25 صيغة (sprängning، borrning، dagvattenhantering، gräsytor...)
- `PhraseMap`: +12 عبارة (va-ledning، fiber och tele، lod-anläggning...)

### حد NormalizedTextSv
- زِيد من 450 إلى **800 حرف** لاستيعاب البيغرام **(يحتاج migration)**

---

## Migrations المطلوبة

يجب تشغيل migration واحدة تشمل الأمرين:
```sql
-- 1. عمود الاستخدام
ALTER TABLE TaskDefinitions ADD UsageCount int NOT NULL DEFAULT 0;

-- 2. توسيع حقل البحث لاستيعاب البيغرام
ALTER TABLE TaskDefinitions ALTER COLUMN NormalizedTextSv nvarchar(800);
```

وبعدها:
```
POST /api/v1/Storages/rebuild-normalized-text
```
لإعادة بناء NormalizedTextSv لجميع المهام القديمة بالبيغرام الجديد.

---

## ما يمكن تحسينه لاحقاً (اختياري)

### 1. 🔵 Boost بالاستخدام (Usage-based Ranking)
**المشكلة:** المهام الأكثر استخداماً في المشاريع الفعلية لا تحظى بأولوية.  
**الحل:** عمود `UsageCount` على `TaskDefinition`، يزداد عند كل إضافة لحساب، يُستخدم كـ tiebreaker في الترتيب.  
**التعقيد:** متوسط.

---

### 2. 🔵 Bigram Indexing
**المشكلة:** "vattenledning rör" (عبارة كاملة) لا يُخزَّن كوحدة واحدة في NormalizedTextSv.  
**الحل:** تخزين أزواج الكلمات المتجاورة إضافةً للكلمات المفردة.  
**التعقيد:** منخفض–متوسط.

---

### 3. 🟡 SQL Full-Text Search
**المشكلة:** `LIKE '%token%'` بطيء على جداول كبيرة جداً (>100,000 صف).  
**الحل:** `CREATE FULLTEXT INDEX` على `NormalizedTextSv` + `Name`، ثم `CONTAINS()` في EF Core.  
**التعقيد:** مرتفع (يتطلب تغيير بنية DB + migrations).  
**الفائدة:** تُصبح تُحسِّن ملحوظة فقط عند >50k مهمة.

---

### 4. 🟡 Incremental TF-IDF Update
**المشكلة:** عند إضافة مهمة جديدة، الـ IDF cache يتجدد كل 30 دقيقة.  
**الحل:** `InvalidateAll()` يُستدعى تلقائياً من `CreateAsync` و`DeleteAsync` في `ProjectTaskService`.  
**الملاحظة:** يحتاج إحقان `ITfIdfIndexProvider` في `ProjectTaskService` — يتطلب التحقق من اتجاه dependencies بين المشاريع.  
**التعقيد:** منخفض.

---

### 5. 🟢 تحسين قاموس المرادفات
**المشكلة:** قاموس المرادفات الحالي (~40 زوج) لا يغطي كل مصطلحات البناء السويدية.  
**الحل:** مراجعة دورية من قِبل خبير لغوي/مجال وإضافة أزواج جديدة في `SwedishTaskTextNormalizer.SynonymMap`.  
**التعقيد:** منعدم (تعديل Dictionary فقط، ثم `POST /rebuild-normalized-text`).

---

### 6. 🟢 تحسين قاموس الجذور المركبة
**المشكلة:** `CompoundRoots` (~60 جذر) قد لا يشمل كل الجذور السويدية في مجال البناء.  
**الحل:** إضافة جذور جديدة حسب الحاجة من واقع البيانات الفعلية.  
**التعقيد:** منعدم.

---

## ملاحظة تشغيلية مهمة

بعد كل تعديل على `SwedishTaskTextNormalizer` (مرادفات جديدة، جذور جديدة، إلخ):

```
POST /api/v1/Storages/rebuild-normalized-text
Authorization: Bearer <admin-token>
```

هذا يُعيد بناء `NormalizedTextSv` لجميع المهام القديمة ويُفرغ الـ TF-IDF cache تلقائياً.
