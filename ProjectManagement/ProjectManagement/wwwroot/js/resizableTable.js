window.nelCalcDotNetRef = null;

// Persists column widths across Blazor re-renders (Blazor resets inline styles on every render)
const _pmSavedWidths = {};
let _pmSavedTableWidth = 0;

window.initializeResizableColumns = function (dotNetRef) {
    window.nelCalcDotNetRef = dotNetRef;

    const table = document.getElementById('resizeMe');
    if (!table) return;

    createResizableTable(table);
    restoreColumnWidths(table);
    observeFrozenColumns(table);
    queueFrozenSync(table);
    preventSelectAll(table);

    const scrollContainer = table.closest('.divNetCalc');
    if (scrollContainer) preventHorizontalBackNavigation(scrollContainer);
};

// ─── column resizing ──────────────────────────────────────────────────────────

const createResizableTable = (table) => {
    const cols = table.querySelectorAll('thead th');
    cols.forEach(col => {
        if (col.querySelector(':scope > .resizer'))
            return;

        // Store the original CSS min-width once, before any resize changes it
        if (!col.dataset.pmOrigMinWidth) {
            const mw = parseFloat(window.getComputedStyle(col).minWidth);
            col.dataset.pmOrigMinWidth = (Number.isFinite(mw) && mw > 0) ? mw : 48;
        }

        // Ensure th is a positioning context for the absolute-positioned resizer
        const pos = window.getComputedStyle(col).position;
        if (pos === 'static') col.style.position = 'relative';

        const resizer = document.createElement('div');
        resizer.classList.add('resizer');
        col.appendChild(resizer);
        createResizableColumn(col, resizer);
    });
};

const createResizableColumn = function (col, resizer) {
    let x = 0;
    let w = 0;
    let tableW = 0;
    let minW = 0;
    let elementID;

    const mouseDownHandler = function (e) {
        e.stopPropagation();
        e.preventDefault();
        elementID = col.id.replace('h', '');
        x = e.clientX;
        w = parseInt(window.getComputedStyle(col).width, 10);
        tableW = Math.ceil(col.closest('table')?.getBoundingClientRect().width || 0);
        minW = getColumnMinWidth(col);
        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler, { once: true });
        resizer.classList.add('resizing');
    };

    const mouseMoveHandler = function (e) {
        const table = col.closest('table');
        const newWidth = Math.max(minW, w + e.clientX - x);
        applyColumnWidth(col, newWidth);

        if (table) {
            const newTableWidth = Math.max(table.parentElement?.clientWidth || 0, tableW + newWidth - w);
            table.style.width = `${newTableWidth}px`;
            table.style.minWidth = '100%';
        }

        queueFrozenSync(table);
    };

    const mouseUpHandler = () => {
        resizer.classList.remove('resizing');
        const tbl = col.closest('table');
        const newWidth = parseInt(window.getComputedStyle(col).width, 10);
        _pmSavedWidths[elementID] = newWidth;
        if (tbl) _pmSavedTableWidth = parseInt(tbl.style.width, 10) || 0;
        SaveTemplateJs(`${elementID}||${newWidth}`);
        queueFrozenSync(tbl);
        document.removeEventListener('mousemove', mouseMoveHandler);
    };

    resizer.addEventListener('mousedown', mouseDownHandler);
    resizer.addEventListener('click', e => e.stopPropagation());
};

function getColumnMinWidth(col) {
    if (col.dataset.pmOrigMinWidth) return parseFloat(col.dataset.pmOrigMinWidth);
    const minWidth = parseFloat(window.getComputedStyle(col).minWidth);
    if (Number.isFinite(minWidth) && minWidth > 0) return minWidth;
    return 48;
}

function restoreColumnWidths(table) {
    if (!table) return;
    Array.from(table.querySelectorAll('thead th')).forEach(th => {
        const id = (th.id || '').replace(/^h/, '');
        if (id && _pmSavedWidths[id] > 0) {
            applyColumnWidth(th, _pmSavedWidths[id]);
        }
    });
    if (_pmSavedTableWidth > 0) {
        table.style.width = `${_pmSavedTableWidth}px`;
        table.style.minWidth = '100%';
    }
}

function applyColumnWidth(header, width) {
    const table = header.closest('table');
    if (!table) return;

    const columnIndex = Array.from(header.parentElement.children).indexOf(header);
    if (columnIndex < 0) return;

    table.querySelectorAll('tr').forEach(row => {
        const cell = row.children[columnIndex];
        if (!cell) return;

        cell.style.width = `${width}px`;
        cell.style.minWidth = `${width}px`;
    });
}

// ─── width helper ─────────────────────────────────────────────────────────────

function getFrozenWidth(el) {
    const layoutWidth = el.getBoundingClientRect().width;
    if (Number.isFinite(layoutWidth) && layoutWidth > 0) {
        return layoutWidth;
    }
    const computedWidth = parseFloat(window.getComputedStyle(el).width);
    return Number.isNaN(computedWidth) ? 0 : computedWidth;
}

// ─── CSS-variable sync (kept for external consumers) ─────────────────────────

function syncStickyTopOffsets(table) {
    const scrollContainer = table.closest('.divNetCalc');
    if (!scrollContainer) return;

    const thead = table.tHead;
    if (!thead) return;

    const headerRow = table.querySelector('thead #header-row') || thead.rows[0];
    const headerH   = headerRow ? Math.round(headerRow.getBoundingClientRect().height) : 22;

    const filterRow = thead.querySelector('#headerFilter');
    const filterH   = filterRow ? Math.round(filterRow.getBoundingClientRect().height) : 0;

    scrollContainer.style.setProperty('--pm-th-height',     `${headerH}px`);
    scrollContainer.style.setProperty('--pm-filter-height', `${filterH}px`);
}

// ─── queue + observe ─────────────────────────────────────────────────────────

function queueFrozenSync(table) {
    if (!table) return;

    if (table._pmFrozenFrame)  window.cancelAnimationFrame(table._pmFrozenFrame);
    if (table._pmFrozenTimer)  window.clearTimeout(table._pmFrozenTimer);

    table._pmFrozenFrame = window.requestAnimationFrame(() => {
        syncStickyTopOffsets(table);
        syncFrozenColumns(table);
    });
    table._pmFrozenTimer = window.setTimeout(() => {
        syncStickyTopOffsets(table);
        syncFrozenColumns(table);
    }, 40);
}

function observeFrozenColumns(table) {
    if (!table || table._pmFrozenObserver) return;

    const tbody = table.tBodies?.[0];
    const thead = table.tHead;

    const observer = new MutationObserver(() => queueFrozenSync(table));

    if (tbody) observer.observe(tbody, { childList: true, subtree: true });
    if (thead) observer.observe(thead, { childList: true, subtree: false });

    table._pmFrozenObserver = observer;

    if (window.ResizeObserver && !table._pmFrozenResizeObserver) {
        const ro = new ResizeObserver(() => queueFrozenSync(table));
        ro.observe(table);
        if (thead) ro.observe(thead);
        table._pmFrozenResizeObserver = ro;
    }
}

// ─── helpers: reset / apply ───────────────────────────────────────────────────

function resetStickyCell(cell) {
    cell.style.position    = '';
    cell.style.top         = '';
    cell.style.left        = '';
    cell.style.zIndex      = '';
    cell.style.background  = '';
    cell.style.boxShadow   = '';
    cell.style.backgroundClip = '';
    cell.classList.remove('pm-frozen-col', 'pm-frozen-header');
}

function clearFrozenColumns(table) {
    // Clear all previously-frozen cells
    table.querySelectorAll('.pm-frozen-col').forEach(resetStickyCell);

    // Clear non-frozen cells in the sum row that got sticky-top applied
    const sumRow = table.querySelector('tbody tr.calc-summary-row');
    if (sumRow) {
        Array.from(sumRow.children).forEach(cell => {
            if (!cell.classList.contains('pm-frozen-col')) resetStickyCell(cell);
        });
    }

    // Clear non-frozen cells in the filter row that got sticky-top applied
    const filterRow = table.querySelector('thead tr#headerFilter');
    if (filterRow) {
        Array.from(filterRow.children).forEach(cell => {
            if (!cell.classList.contains('pm-frozen-col')) resetStickyCell(cell);
        });
    }
}

function applyFrozenColumn(el, left, isHeader, order) {
    el.classList.add('pm-frozen-col');
    if (isHeader) el.classList.add('pm-frozen-header');
    el.style.position = 'sticky';
    el.style.left     = `${left}px`;
    el.style.zIndex   = isHeader ? `${70 - order}` : `${10 - order}`;
}

/**
 * Make every cell in `cells` stick to `topPx` vertically.
 * Frozen cells (already have .pm-frozen-col + left) get a higher z-index
 * so they sit above the non-frozen sticky cells in the same row.
 */
function applyRowStickyTop(cells, topPx, frozenZIndex, normalZIndex) {
    cells.forEach(cell => {
        cell.style.position       = 'sticky';
        cell.style.top            = topPx + 'px';
        cell.style.zIndex         = cell.classList.contains('pm-frozen-col')
            ? String(frozenZIndex)
            : String(normalZIndex);
        cell.style.backgroundClip = 'padding-box';
    });
}

// ─── main sync ────────────────────────────────────────────────────────────────

function getFrozenHeaders(table) {
    return Array.from(table.querySelectorAll('thead th'))
        .map((th, index) => ({ th, index }))
        .filter(({ th }) => th.dataset.pmFrozen === 'true');
}

function syncFrozenColumns(table) {
    if (!table) return;

    clearFrozenColumns(table);

    const frozenHeaders = getFrozenHeaders(table);
    const filterRow     = table.querySelector('thead tr#headerFilter');
    const sumRow        = table.querySelector('tbody tr.calc-summary-row');
    const allBodyRows   = Array.from(table.querySelectorAll('tbody tr'));
    const regularRows   = allBodyRows.filter(r => r !== sumRow);

    let frozenLeft = 0;

    // 1. Apply frozen-left to header, regular body rows, sum row, filter row
    frozenHeaders.forEach(({ th, index }, order) => {
        applyFrozenColumn(th, frozenLeft, true, order);

        regularRows.forEach(row => {
            const cell = row.children[index];
            if (cell) applyFrozenColumn(cell, frozenLeft, false, order);
        });

        if (sumRow) {
            const cell = sumRow.children[index];
            if (cell) applyFrozenColumn(cell, frozenLeft, false, order);
        }

        if (filterRow) {
            const cell = filterRow.children[index];
            if (cell) applyFrozenColumn(cell, frozenLeft, false, order);
        }

        frozenLeft += getFrozenWidth(th);
    });

    // 2. Make filter row sticky below the main header row
    if (filterRow) {
        const headerRow  = table.querySelector('thead tr#header-row');
        const headerRowH = headerRow
            ? Math.round(headerRow.getBoundingClientRect().height)
            : 22;

        // z-index ladder:
        //   frozen filter cell  → 35  (above frozen body 20, below frozen th 40)
        //   normal  filter cell →  9  (above body, below th 10)
        applyRowStickyTop(Array.from(filterRow.children), headerRowH, 35, 9);

        Array.from(filterRow.children).forEach(cell => {
            if (!cell.style.background)
                cell.style.background = 'inherit';
        });
    }

    // 3. Make sum row sticky below the entire thead (header + optional filter)
    if (sumRow) {
        const theadH = table.tHead
            ? Math.round(table.tHead.getBoundingClientRect().height)
            : 22;

        // z-index ladder:
        //   frozen sum cell  → 25  (above frozen body 20, below frozen filter 35)
        //   normal  sum cell →  8  (above body, below filter 9)
        applyRowStickyTop(Array.from(sumRow.children), theadH, 25, 8);

        // Subtle bottom shadow so the sum row visually separates from the data
        Array.from(sumRow.children).forEach(cell => {
            cell.style.boxShadow = '0 1px 0 rgba(15,23,42,0.10)';
        });
    }

    syncOverflowTitles(table);
}

// ─── overflow titles ─────────────────────────────────────────────────────────

function syncOverflowTitles(table) {
    table.querySelectorAll('[data-pm-overflow-title]').forEach(syncOverflowTitle);
}

function syncOverflowTitle(cell) {
    const fullText = cell.dataset.pmOverflowTitle;
    if (!fullText) { cell.removeAttribute('title'); return; }

    const isOverflowing = Math.ceil(cell.scrollWidth) > Math.ceil(cell.clientWidth) + 1;
    if (isOverflowing) cell.setAttribute('title', fullText);
    else               cell.removeAttribute('title');
}

// ─── Blazor bridge ───────────────────────────────────────────────────────────

function SaveTemplateJs(event) {
    if (!window.nelCalcDotNetRef) {
        console.warn('nelCalcDotNetRef is not set');
        return;
    }
    window.nelCalcDotNetRef.invokeMethodAsync('SaveTemplateBlazor', event);
}
