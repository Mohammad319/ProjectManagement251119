window.nelCalcDotNetRef = null;

window.initializeResizableColumns = function (dotNetRef) {
    window.nelCalcDotNetRef = dotNetRef;

    const table = document.getElementById('resizeMe');
    if (!table) return;

    createResizableTable(table);

    window.requestAnimationFrame(() => {
        syncFrozenColumns(table);
        window.setTimeout(() => syncFrozenColumns(table), 40);
    });
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
        ResetNeetCalcTable(parseInt(elementID, 10) + 1);
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
        SetNewNTHChild(+elementID + 1, newWidth - w);
        syncFrozenColumns(document.getElementById('resizeMe'));
        document.removeEventListener('mousemove', mouseMoveHandler);
    };

    resizer.addEventListener('mousedown', mouseDownHandler);
};

var lefts = [];

const ResetNeetCalcTable = (index) => {
    const table = document.getElementById('resizeMe');
    if (!table) return;

    const headers = table.querySelectorAll('thead th');

    for (let i = index; i < headers.length; i++) {
        const prevLeft = parseInt(window.getComputedStyle(headers[i - 1]).left, 10);
        lefts.push(Number.isNaN(prevLeft) ? NaN : prevLeft);
        headers[i - 1].style.left = 'auto';

        table.querySelectorAll(`tbody tr.calc-data-row > td:nth-child(${i})`).forEach(td => {
            td.style.left = 'auto';
        });
    }
};

const SetNewNTHChild = (index, plusLeft) => {
    const table = document.getElementById('resizeMe');
    if (!table) return;

    const headers = table.querySelectorAll('thead th');
    for (let i = index; i < headers.length; i++) {
        let left = lefts[i - index];
        if (!Number.isNaN(left) && left >= 0) {
            left += plusLeft;
            headers[i - 1].style.left = `${left}px`;

            table.querySelectorAll(`tbody tr.calc-data-row > td:nth-child(${i})`).forEach(td => {
                td.style.left = `${left}px`;
            });
        }
    }
    lefts = [];
};

function clearFrozenColumns(table) {
    table.querySelectorAll('.pm-frozen-col').forEach(el => {
        el.classList.remove('pm-frozen-col', 'pm-frozen-header');
        el.style.position = '';
        el.style.zIndex = '';
        el.style.background = '';
    });
}

function isTransparent(color) {
    return !color || color === 'transparent' || color === 'rgba(0, 0, 0, 0)';
}

function getFrozenBackground(el, isHeader) {
    if (isHeader) {
        return 'var(--net-header-bg)';
    }

    const ownBg = window.getComputedStyle(el).backgroundColor;
    if (!isTransparent(ownBg)) {
        return ownBg;
    }

    const row = el.parentElement;
    if (row) {
        const rowBg = window.getComputedStyle(row).backgroundColor;
        if (!isTransparent(rowBg)) {
            return rowBg;
        }
    }

    const table = el.closest('table');
    if (table) {
        const tableBg = window.getComputedStyle(table).backgroundColor;
        if (!isTransparent(tableBg)) {
            return tableBg;
        }
    }

    return '#ffffff';
}

function applyFrozenColumn(el, left, isHeader, order) {
    el.classList.add('pm-frozen-col');
    if (isHeader) el.classList.add('pm-frozen-header');

    el.style.position = 'sticky';
    el.style.left = `${left}px`;
    el.style.zIndex = isHeader ? `${40 - order}` : `${20 - order}`;
    el.style.background = getFrozenBackground(el, isHeader);
}

function syncFrozenColumns(table) {
    if (!table) return;

    clearFrozenColumns(table);

    const headers = Array.from(table.querySelectorAll('thead th'));
    headers.forEach((th, index) => {
        const rawLeft = window.getComputedStyle(th).left;
        const left = parseInt(rawLeft, 10);

        if (Number.isNaN(left))
            return;

        applyFrozenColumn(th, left, true, index);

        table.querySelectorAll(`tbody tr.calc-data-row > td:nth-child(${index + 1})`).forEach(td => {
            applyFrozenColumn(td, left, false, index);
        });
    });
}

function SaveTemplateJs(event) {
    if (!window.nelCalcDotNetRef) {
        console.warn('nelCalcDotNetRef is not set');
        return;
    }

    window.nelCalcDotNetRef.invokeMethodAsync('SaveTemplateBlazor', event);
}