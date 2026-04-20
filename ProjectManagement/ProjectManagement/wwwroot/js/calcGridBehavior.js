window.clampCalcGridScroll = function () {
    const el = document.querySelector('.divNetCalc');
    if (!el) return;
    const maxScroll = el.scrollHeight - el.clientHeight;
    if (el.scrollTop > maxScroll) {
        el.scrollTop = Math.max(0, maxScroll);
    }
};

function preventSelectAll(table) {
    if (!table || table._pmSelectAllBlocked) return;
    table._pmSelectAllBlocked = true;
    table.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'a') {
            e.preventDefault();
        }
    });
}

function preventHorizontalBackNavigation(el) {
    if (!el || el._pmWheelBlocked) return;
    el._pmWheelBlocked = true;
    el.addEventListener('wheel', (e) => {
        if (e.deltaX === 0) return;
        const atLeft = el.scrollLeft <= 0 && e.deltaX < 0;
        const atRight = el.scrollLeft >= el.scrollWidth - el.clientWidth && e.deltaX > 0;
        if (atLeft || atRight) e.preventDefault();
    }, { passive: false });
}
