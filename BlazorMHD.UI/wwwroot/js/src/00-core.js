(function (window) {
    const root = window.BlazorMHDUI = window.BlazorMHDUI || {};
    const internal = root._internal = root._internal || {};

    internal.clamp = function clamp(v, min, max) {
        return Math.max(min, Math.min(max, v));
    };

    internal.ensureRelative = function ensureRelative(el) {
        if (getComputedStyle(el).position === 'static') {
            el.style.position = 'relative';
        }
    };
})(window);
