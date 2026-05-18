توثيق بسيط لاعمدة الاستيراد
============================

ملاحظة عامة
-----------
ترتيب الاعمدة غير مهم اذا كان الصف الاول يحتوي على اسماء الاعمدة بوضوح.
المهم هو اسم العمود وليس مكانه.

اذا كان الملف بدون عناوين اعمدة، عندها يصبح الترتيب مهما، لذلك يفضل دائما وضع صف عناوين في اول الملف.


اولا: صفحة Clean Data
---------------------
هذه الصفحة مخصصة للبيانات النظيفة والتدريب والبحث.
يمكن استخدام نفس القالب للمهام فقط او للمهام مع الموارد.

الاعمدة المقترحة:

RowType
Code
ParentCode
Name
Unit
TaskNameSynonym1
TaskNameSynonym2
TaskUnitSynonym1
TaskUnitSynonym2
ResourceType
UnitCost

شرح الاعمدة:

RowType
T يعني صف مهمة.
R يعني صف مورد تابع لمهمة.

Code
كود المهمة في صفوف T.

ParentCode
كود المهمة الاب في صفوف R.

Name
في صف T يعني اسم المهمة.
في صف R يعني اسم المورد.

Unit
في صف T يعني وحدة المهمة.
في صف R يعني وحدة المورد.

TaskNameSynonym1 / TaskNameSynonym2
مرادفات او صيغ بديلة لاسم المهمة.

TaskUnitSynonym1 / TaskUnitSynonym2
مرادفات او صيغ بديلة لوحدة المهمة.

ResourceType
نوع المورد في صفوف R، مثل:
Material
Machine
Worker
Subcontractor

UnitCost
تكلفة وحدة المورد، وهي اختيارية.

Quantity
ليست مطلوبة في قالب Clean Data الاساسي.
اذا وجدت في الملف، سيحاول النظام قراءتها، لكنها ليست مهمة للتدريب لان كمية المشروع تأتي عادة من المستخدم لاحقا.

مثال Clean Data - مهام فقط:

RowType;Code;ParentCode;Name;Unit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceType;UnitCost
T;BBB.131;;Geotekniska forhallanden i jord;belopp;Geoteknisk undersokning;Jordmekanik;projektenhet;egendefinierad enhet;;
T;BBB.132;;Geotekniska forhallanden i berg;belopp;Bergteknik;Bergundersokning;projektenhet;egendefinierad enhet;;

مثال Clean Data - مهام مع موارد:

RowType;Code;ParentCode;Name;Unit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceType;UnitCost
T;BBB.131;;Geotekniska forhallanden i jord;belopp;Geoteknisk undersokning;Jordmekanik;projektenhet;egendefinierad enhet;;
R;;BBB.131;Anlaggare;h;;;;;Worker;610
R;;BBB.131;Servicebil;h;;;;;Machine;210


ثانيا: صفحة CSV Import
----------------------
هذه الصفحة مخصصة للاستيراد الكامل للمهام والموارد والوصفات والتفاصيل.
هي اقوى من Clean Data لكنها تحتاج ملفا منظما اكثر.

الاعمدة المقترحة:

Radtyp
TaskId
ParentTaskId
ParentCode
Code
Name
TaskNameSynonym1
TaskNameSynonym2
Quantity
Unit
TaskUnitSynonym1
TaskUnitSynonym2
Property1
Value1
Property2
Value2
Account
ResourceType
ResourceFolder
CapWaste
CapWasteType
QuantityFactor
UnitCost
Category
CalculationMethod
ResourceRowId
ParameterType
ParameterValue

شرح اهم الاعمدة:

Radtyp
T يعني مهمة.
R يعني مورد.
P يعني باراميتر.

Code
كود المهمة في صفوف T.

ParentCode
كود المهمة الاب في صفوف R.

Name
في صف T يعني اسم المهمة.
في صف R يعني اسم المورد.
في صف P يعني اسم الباراميتر.

Quantity
كمية المهمة او المورد حسب نوع الصف.

Unit
وحدة المهمة او المورد حسب نوع الصف.

TaskNameSynonym1 / TaskNameSynonym2
مرادفات اسم المهمة، وتقرأ عادة من صفوف T.

TaskUnitSynonym1 / TaskUnitSynonym2
مرادفات وحدة المهمة، وتقرأ عادة من صفوف T.

ResourceType
نوع المورد في صفوف R.

ResourceFolder
مجلد المورد، مثل:
Personal > Mark > Anlaggare

UnitCost
تكلفة وحدة المورد.

QuantityFactor
عامل حسابي اختياري للمورد.

Category
تصنيف اختياري.

CalculationMethod
ملاحظة او طريقة حساب اختيارية.

ResourceRowId
معرف صف المورد، يفيد عند ربط صفوف P بالموارد.

ParameterType / ParameterValue
تستخدم مع صفوف P اذا كان الملف يحتوي باراميترات.

مثال CSV Import:

Radtyp;Code;ParentCode;Name;Quantity;Unit;TaskNameSynonym1;TaskNameSynonym2;TaskUnitSynonym1;TaskUnitSynonym2;ResourceType;ResourceFolder;UnitCost;QuantityFactor
T;BBB.131;;Geotekniska forhallanden i jord;1;belopp;Geoteknisk undersokning;Jordmekanik;projektenhet;egendefinierad enhet;;;;
R;;BBB.131;Anlaggare;1;h;;;;;Worker;Personal > Mark > Anlaggare;610;1
R;;BBB.131;Servicebil;1;h;;;;;Machine;Maskiner > Servicebil;210;1


الفرق المختصر
-------------
Clean Data:
للتدريب والبيانات النظيفة.
اعمدة قليلة.
Quantity غير مهمة غالبا.

CSV Import:
للاستيراد الكامل.
يدعم موارد ومجلدات وباراميترات وتفاصيل اكثر.
يتطلب RowType/Radtyp واضحا، خصوصا T للمهام و R للموارد.
