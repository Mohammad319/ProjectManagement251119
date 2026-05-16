# تقرير: الاستدلال اللغوي في نظام إدارة المشاريع

**تاريخ التقرير**: 2026-05-16  
**الفرع**: b16  
**المشروع**: ProjectManagement251119

---

## 1. ملخص تنفيذي

يطبّق النظام محرك استدلال لغوي متكامل للغة السويدية، مصمم خصيصاً للقطاع الإنشائي. يجمع المحرك بين تقنيات TF-IDF، والبحث الغامض (Fuzzy Search)، وتطبيع النصوص السويدية، بهدف مطابقة مهام المشروع بمواردها بدقة عالية.

---

## 2. المكوّنات الأساسية

### 2.1 `SwedishTaskTextNormalizer.cs`
**المسار**: `ProjectManagement.Shared/Helper/Text/SwedishTaskTextNormalizer.cs`

المحرك الرئيسي للتطبيع اللغوي. يحتوي على:

#### أ) الجذور المركّبة (CompoundRoots) — 80+ جذر
كلمات إنشائية سويدية متخصصة تشمل:
- نظم المياه: `vatten`, `avlopp`, `dränering`, `dagvatten`
- الحفر والأساسات: `schakt`, `grund`, `pål`
- المواد: `betong`, `armering`, `asfalt`
- البنية: `vägg`, `tak`, `golv`
- التخضير: `plantering`, `gräs`

#### ب) المرادفات الدلالية (SynonymMap) — 100+ تعيين
تعيينات ثنائية الاتجاه تربط المصطلحات المتكافئة:
- `regnvatten` ↔ `dagvatten`
- `kanalisation` ↔ `avlopp`
- `markarbete` ↔ `schakt`
- `sprutbetong` ↔ `betong`

#### ج) الكلمات المحجوبة (StopWords) — 50+ كلمة
كلمات وظيفية سويدية وإنجليزية وهولندية تُحذف قبل التحليل.

#### د) خريطة الجمل (PhraseMap) — 30+ عبارة
تفكيك الكلمات المركّبة الطويلة:
- `grundschaktning` → `grund schakt`
- `dagvattenledning` → `dagvatten ledning`
- `va-ledning` → `vatten avlopp ledning`

#### هـ) خريطة التوكنات (TokenMap) — 120+ قاعدة
تحويل التصريفات والمشتقات:
- `schaktning` → `schakt`
- `betongarbete` → `betong`
- `dräneringsrör` → `dränering rör`

#### و) تقسيم الكلمات المركبة (Compound Splitting)
- تحليل ذكي لأجزاء متعددة: `dagvattenledningsgrav` → `[dagvatten, ledning, grav]`
- معالجة المورفيمات الرابطة: `nings`, `ings`, `s`, `e`

#### ز) الثنائيات (Bigrams)
- توليد عبارات من كلمتين: `schakt rör` → `schakt_rör` + `rör_schakt`
- تُستخدم كتوكن ثالث في استعلامات البحث

---

### 2.2 خوارزمية حساب التشابه (CalculateSimilarity)

**المعادلة الرئيسية**:
```
Score = (0.30 × Jaccard_Weighted) + (0.60 × Coverage) + (0.10 × ContainsBonus)
```

| المكوّن | الوزن | الوصف |
|---------|-------|-------|
| Jaccard الموزون | 30% | تقاطع التوكنات ÷ اتحادها (مع أوزان TF-IDF) |
| Coverage | 60% | جزء توكنات الاستعلام الموجود في الهدف |
| Contains Bonus | 10% | +0.10 إذا كان أحد النصين يحتوي على الآخر |

**أوزان التوكنات (TokenImportance)**:
| نوع التوكن | الوزن |
|-----------|-------|
| أكواد ألفا-رقمية (مثل `VA-100`) | 3.0× |
| أرقام بحتة (مثل `110`, `220`) | 2.0× |
| كلمات طويلة (≥ 6 أحرف) | 1.2× |
| كلمات أخرى | 1.0× |

**البحث الغامض بالأحرف (Damerau-Levenshtein)**:
- يُفعَّل عند عتبة تشابه 0.70
- يصحح الأخطاء الإملائية: `betnog` ≈ `betong`

---

### 2.3 `ITfIdfIndexProvider.cs`
**المسار**: `Application/Feature/TfIdf/ITfIdfIndexProvider.cs`

واجهة موفر فهرس TF-IDF:
- `GetIdfScoresAsync(int tenantId)` — استرجاع درجات IDF المحسوبة مسبقاً
- `InvalidateAll()` — إبطال الذاكرة المؤقتة لإعادة الحساب

---

### 2.4 `StorageRepository.cs`
**المسار**: `ProjectManagement.Client.Shared/Repositories/Calculation/Implement/StorageRepository.cs`

تنفيذ عميل HTTP لاسترجاع درجات TF-IDF:
```csharp
public async Task<Dictionary<string, double>> GetTfIdfAsync()
    => await _httpRepository.GetAsync<Dictionary<string, double>>(StorageURLBase + "tfidf")
       ?? new Dictionary<string, double>();
```

---

### 2.5 `ITaskDefinitionService.cs`
**المسار**: `TaskResourceBlueprints/Services/ProjectTask/ITaskDefinitionService.cs`

خدمة تعريف المهام مع دعم البحث بالتوكنات:

| الطريقة | الوصف |
|--------|-------|
| `GetTasksForUserDtoAsync(filter)` | البحث بـ `SearchTokens` (حتى 3 توكنات) |
| `RebuildNormalizedTextAsync()` | إعادة بناء حقل `NormalizedTextSv` |
| `IncrementUsageAsync(taskId)` | تتبع استخدام المهام |

**منطق البحث**:
```
1 توكن:  WHERE NormalizedTextSv CONTAINS t0 OR Name CONTAINS t0
2 توكن:  OR (t0) OR (t1)
3 توكن:  OR (t0) OR (t1) OR (t2=bigram)
```

---

### 2.6 واجهات المستخدم (Razor Components)

**`AllTasksTopSuggestionsDialog.razor`**  
عرض جميع المهام الورقية مع مقترحات الموارد:
- تصفية بـ Fuzzy Search الفوري
- شريط تمرير لدرجة الثقة (0-100%)
- ألوان حسب الدرجة: أخضر ≥ 0.80، أصفر ≥ 0.55، أحمر < 0.55

**`TaskResourceSuggestionsDialog.razor`**  
عرض 30 مقترح لمهمة واحدة:
- حساب كمية ذكي حسب نوع المورد (هدر، سعة، عادي)
- تحويل وحدات ديناميكي

---

## 3. التغييرات الأخيرة

### 3.1 تحسين خوارزمية التشابه

**التغيير الرئيسي**: انتقال من Jaccard البسيط إلى Coverage كمعيار أولوي.

| البُعد | القيمة القديمة | القيمة الجديدة |
|--------|--------------|--------------|
| وزن Jaccard | 60% | 30% |
| وزن Coverage | 30% | **60%** |
| وزن Contains | 10% | 10% |

**السبب**: الاستعلامات القصيرة الموجودة بالكامل في الهدف يجب أن تسجّل درجة عالية، حتى لو لم تكن مطابقة تامة.

### 3.2 إضافة أوزان التوكنات

```csharp
// جديد: TokenizeWeighted بدلاً من Tokenize
private static Dictionary<string, double> TokenizeWeighted(string text)
{
    foreach (var token in tokens)
        result.TryAdd(token, TokenImportance(token));
}
```

### 3.3 توسيع Stop Words

إضافة 20+ كلمة إنجليزية وهولندية وألمانية لتغطية قوائم الأسعار الدولية.

### 3.4 ملف جديد: `ITfIdfIndexProvider.cs`

واجهة جديدة لفصل منطق الفهرسة عن باقي النظام.

---

## 4. تدفق النظام الكامل

```
المستخدم يكتب استعلاماً
        │
        ▼
SwedishTaskTextNormalizer.Normalize()
  - توسيع المرادفات
  - تقسيم الكلمات المركبة
  - تطبيق قواعد TokenMap
  - توليد Bigrams
        │
        ▼
ProjectTaskFilterDto { SearchTokens: [t0, t1, t2] }
        │
        ▼
ITaskDefinitionService.GetTasksForUserDtoAsync()
  - بحث OR في NormalizedTextSv + Name + Code
  - ترتيب: UsageCount → Code → Name
        │
        ▼
نتائج مرتّبة تُعرض في واجهة المستخدم
  - FuzzySearchHelper يصفّي العرض
  - ألوان حسب درجة التشابه
```

---

## 5. مثال عملي

**الاستعلام**: `"schakt rör"`

```
1. Normalize("schakt rör")
   → tokens: ["schakt", "rör"]
   → bigram: "schakt_rör"
   → SearchTokens: ["schakt", "rör", "schakt_rör"]

2. DB Query:
   WHERE NormalizedTextSv CONTAINS "schakt"
      OR NormalizedTextSv CONTAINS "rör"
      OR NormalizedTextSv CONTAINS "schakt_rör"

3. Client-side Scoring:
   "schaktning för rörledning" → Score: 0.82 (أخضر)
   "schakt" فقط              → Score: 0.45 (أصفر)
```

---

## 6. الخلاصة

النظام يحقق توازناً بين:
- **الدقة اللغوية**: 200+ قاعدة سويدية متخصصة للقطاع الإنشائي
- **الأداء**: حقل `NormalizedTextSv` محسوب مسبقاً في قاعدة البيانات
- **تجربة المستخدم**: بحث فوري من جانب العميل بدون طلبات سيرفر إضافية
- **المرونة**: تحويل وحدات ديناميكي وحساب موارد معقد

---

*تم إنشاء هذا التقرير تلقائياً بتاريخ 2026-05-16*
