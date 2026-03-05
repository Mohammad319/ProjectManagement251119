namespace Persistence.Configurations;

// هذا الملف كان يُستخدم سابقاً لتجميع Configurations إضافية في ملف واحد.
// بعد إضافة ملفات Configuration متخصصة (AccountingConfiguration/OrganisationConfiguration/
// ApplicationConfiguration/CalculationSupportConfiguration/ProjectLookupsConfiguration)
// أصبح وجود نفس الـ classes هنا يسبب تعارضات (CS0101/CS0111).
//
// نترك الملف كـ marker فقط لتجنب كسر أي مراجع/دمج قديم.
internal static class AdditionalEntityConfigurationsMarker { }
