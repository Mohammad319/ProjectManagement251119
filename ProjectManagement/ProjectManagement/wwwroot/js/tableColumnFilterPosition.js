window.tableColumnFilterPosition = {
    calculate: function (button, popupWidth, gap) {
        const fallback = { x: gap, y: gap };
        if (!button || typeof button.getBoundingClientRect !== 'function') {
            return fallback;
        }

        const buttonRect = button.getBoundingClientRect();
        const headerCell = button.closest('th');
        const isFrozenHeader = !!headerCell?.matches('[data-pm-frozen="true"]');
        const frozenHeaders = headerCell?.parentElement
            ? Array.from(headerCell.parentElement.children).filter(cell => cell.matches('[data-pm-frozen="true"]'))
            : [];
        const isFirstFrozenHeader = isFrozenHeader && frozenHeaders.indexOf(headerCell) === 0;

        let x = buttonRect.left;
        if (isFirstFrozenHeader) {
            const headerRect = headerCell.getBoundingClientRect();
            x = Math.max(buttonRect.right + gap, headerRect.right + gap);
        }

        const viewportWidth = document.documentElement.clientWidth || window.innerWidth || 0;
        const maxX = Math.max(gap, viewportWidth - popupWidth - gap);

        return {
            x: Math.min(Math.max(gap, x), maxX),
            y: buttonRect.bottom + gap
        };
    }
};
