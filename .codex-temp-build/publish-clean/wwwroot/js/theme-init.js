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

// تشغيل فوري عند تحميل السكريبت (قبل رسم أي محتوى) لمنع وميض الضوء
window.applyThemeFromStorage();

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

// تشغيل بعد كل تنقّل محسّن من Blazor
document.addEventListener('DOMContentLoaded', function () {
    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', function () {
            window.applyThemeFromStorage();
        });
    }
});
