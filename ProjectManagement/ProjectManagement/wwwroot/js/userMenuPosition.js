window.projectManagementUserMenu = (function () {
    let escapeHandler = null;

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function getPosition(trigger, preferredWidth) {
        const margin = 8;
        const rect = trigger.getBoundingClientRect();
        const width = Math.min(preferredWidth || 224, window.innerWidth - (margin * 2));
        const left = clamp(rect.right - width, margin, window.innerWidth - width - margin);
        const top = Math.min(rect.bottom + 8, window.innerHeight - margin);
        const maxHeight = Math.max(160, window.innerHeight - top - margin);

        return { top, left, width, maxHeight };
    }

    function enableEscape(dotNetRef) {
        disableEscape();
        escapeHandler = function (event) {
            if (event.key === 'Escape') {
                dotNetRef.invokeMethodAsync('CloseProfileMenuFromJs').catch(function () { });
            }
        };
        document.addEventListener('keydown', escapeHandler, true);
    }

    function disableEscape() {
        if (escapeHandler) {
            document.removeEventListener('keydown', escapeHandler, true);
            escapeHandler = null;
        }
    }

    return {
        getPosition,
        enableEscape,
        disableEscape
    };
})();
