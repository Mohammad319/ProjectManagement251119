module.exports = {
    darkMode: 'class',
    content: [
        // صفحات Razor
        "./Pages/**/*.{razor,cshtml}",
        "./Components/**/*.{razor,cshtml}",
        "./Areas/**/*.{razor,cshtml}",
        "./Shared/**/*.{razor,cshtml}",

        // ملفات HTML (إن وجدت)
        "./wwwroot/**/*.html",

        // سكربتاتك فقط (ليس node_modules)
        "./wwwroot/js/**/*.{js,ts}",
        // لو عندك مشروع Client منفصل، فعّل السطر التالي مع المسار الصحيح:
        // "./Client/**/*.{js,ts}"
        "./ProjectManagement.Client/**/*.{razor,cshtml}",   // ✅ أضِف هذا السطر
    ],
    theme: { extend: {} },
    plugins: [],
    safelist: [
        "bg-blue-100", "text-blue-700",
        "dark:bg-blue-900/50", "dark:text-blue-300",
        "dark:text-gray-300", "dark:hover:bg-gray-800", "dark:hover:text-blue-400",
    ],
};