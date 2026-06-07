window.tableColumnFilterPosition = {
    calculate: function (button, popupWidth, gap) {
        const fallback = { x: gap, y: gap };
        if (!button || typeof button.getBoundingClientRect !== 'function') {
            return fallback;
        }

        const buttonRect = button.getBoundingClientRect();
        let x = buttonRect.left;

        const viewportWidth = document.documentElement.clientWidth || window.innerWidth || 0;
        const maxX = Math.max(gap, viewportWidth - popupWidth - gap);

        return {
            x: Math.min(Math.max(gap, x), maxX),
            y: buttonRect.bottom + gap
        };
    }
};
