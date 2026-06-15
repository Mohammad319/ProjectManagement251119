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

document.addEventListener('DOMContentLoaded', function () {
    window.applyThemeFromStorage();

    if (window.Blazor && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', function () {
            window.applyThemeFromStorage();
        });
    }
});
