window.nelCalcDotNetRef = null;

window.initializeResizableColumns = function (dotNetRef) {
    window.nelCalcDotNetRef = dotNetRef;

    const table = document.getElementById('resizeMe');
    if (!table) return;

    createResizableTable(table);
    observeFrozenColumns(table);
    queueFrozenSync(table);
};

const createResizableTable = (table) => {
    const cols = table.querySelectorAll('thead th');
    cols.forEach(col => {
        if (col.querySelector(':scope > .resizer'))
            return;

        const resizer = document.createElement('div');
        resizer.classList.add('resizer');
        resizer.style.height = `${table.offsetHeight}px`;
        col.appendChild(resizer);
        createResizableColumn(col, resizer);
    });
};

const createResizableColumn = function (col, resizer) {
    let x = 0;
    let w = 0;
    let elementID;

    const mouseDownHandler = function (e) {
        elementID = col.id.replace('h', '');
        x = e.clientX;
        w = parseInt(window.getComputedStyle(col).width, 10);
        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler, { once: true });
        resizer.classList.add('resizing');
    };

    const mouseMoveHandler = function (e) {
        col.style.width = `${w + e.clientX - x}px`;
    };

    const mouseUpHandler = () => {
        resizer.classList.remove('resizing');
        const newWidth = parseInt(window.getComputedStyle(col).width, 10);
        SaveTemplateJs(`${elementID}||${newWidth}`);
        queueFrozenSync(col.closest('table'));
        document.removeEventListener('mousemove', mouseMoveHandler);
    };

    resizer.addEventListener('mousedown', mouseDownHandler);
};

function getFrozenWidth(el) {
    const layoutWidth = el.getBoundingClientRect().width;
    if (Number.isFinite(layoutWidth) && layoutWidth > 0) {
        return layoutWidth;
    }

    const computedWidth = parseFloat(window.getComputedStyle(el).width);
    return Number.isNaN(computedWidth) ? 0 : computedWidth;
}

function queueFrozenSync(table) {
    if (!table) return;

    if (table._pmFrozenFrame) {
        window.cancelAnimationFrame(table._pmFrozenFrame);
    }

    if (table._pmFrozenTimer) {
        window.clearTimeout(table._pmFrozenTimer);
    }

    table._pmFrozenFrame = window.requestAnimationFrame(() => syncFrozenColumns(table));
    table._pmFrozenTimer = window.setTimeout(() => syncFrozenColumns(table), 40);
}

function observeFrozenColumns(table) {
    if (!table || table._pmFrozenObserver) {
        return;
    }

    const tbody = table.tBodies?.[0];
    if (!tbody) {
        return;
    }

    const mutationObserver = new MutationObserver(() => queueFrozenSync(table));
    mutationObserver.observe(tbody, { childList: true, subtree: true });
    table._pmFrozenObserver = mutationObserver;

    if (window.ResizeObserver && !table._pmFrozenResizeObserver) {
        const resizeObserver = new ResizeObserver(() => queueFrozenSync(table));
        resizeObserver.observe(table);
        resizeObserver.observe(table.tHead ?? table);
        table._pmFrozenResizeObserver = resizeObserver;
    }
}

function clearFrozenColumns(table) {
    table.querySelectorAll('.pm-frozen-col').forEach(el => {
        el.classList.remove('pm-frozen-col', 'pm-frozen-header');
        el.style.position = '';
        el.style.left = '';
        el.style.zIndex = '';
        el.style.background = '';
    });
}

function applyFrozenColumn(el, left, isHeader, order) {
    el.classList.add('pm-frozen-col');
    if (isHeader) el.classList.add('pm-frozen-header');

    el.style.position = 'sticky';
    el.style.left = `${left}px`;
    el.style.zIndex = isHeader ? `${40 - order}` : `${20 - order}`;
}

function getFrozenHeaders(table) {
    return Array.from(table.querySelectorAll('thead th'))
        .map((th, index) => ({ th, index }))
        .filter(({ th }) => th.dataset.pmFrozen === 'true');
}

function syncFrozenColumns(table) {
    if (!table) return;

    clearFrozenColumns(table);

    const frozenHeaders = getFrozenHeaders(table);
    const bodyRows = Array.from(table.querySelectorAll('tbody tr'));
    let frozenLeft = 0;

    frozenHeaders.forEach(({ th, index }, order) => {
        applyFrozenColumn(th, frozenLeft, true, order);

        bodyRows.forEach(row => {
            const cell = row.children[index];
            if (!cell) {
                return;
            }

            applyFrozenColumn(cell, frozenLeft, false, order);
        });

        frozenLeft += getFrozenWidth(th);
    });
}

function SaveTemplateJs(event) {
    if (!window.nelCalcDotNetRef) {
        console.warn('nelCalcDotNetRef is not set');
        return;
    }

    window.nelCalcDotNetRef.invokeMethodAsync('SaveTemplateBlazor', event);
}
