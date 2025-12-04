window.applyThemeFromStorage = function () {
    try {
        var t = localStorage.getItem('theme');
        var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        var isDark = (t === 'dark') || (!t && prefersDark);
        var html = document.documentElement;

        if (isDark) {
            html.classList.add('dark');
            html.setAttribute('data-bs-theme', 'dark');
        } else {
            html.classList.remove('dark');
            html.setAttribute('data-bs-theme', 'light');
        }
    } catch { }
};

window.toggleDarkClass = function () {
    const html = document.documentElement;
    const isDark = html.classList.toggle('dark');
    html.setAttribute('data-bs-theme', isDark ? 'dark' : 'light');
    const mode = isDark ? 'dark' : 'light';
    localStorage.setItem('theme', mode);
    return mode;
};

window.getCurrentTheme = function () {
    return localStorage.getItem('theme') || 'light';
};

// ❗ المهم هنا: تشغيل الثيم عند أول تحميل + بعد كل تنقّل محسّن من Blazor
document.addEventListener('DOMContentLoaded', function () {
    // أول تحميل للصفحة
    window.applyThemeFromStorage();

    // بعد كل enhanced navigation من Blazor (.NET 8/10)
    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', function () {
            window.applyThemeFromStorage();
        });
    }
});