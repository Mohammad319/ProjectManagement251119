window.nelCalcDotNetRef = null;

window.initializeResizableColumns = function (dotNetRef) {
    window.nelCalcDotNetRef = dotNetRef;

    const table = document.getElementById('resizeMe');
    if (!table) return;

    createResizableTable(table);
};

const createResizableTable = (table) => {
    const cols = table.querySelectorAll('th');
    cols.forEach(col => {
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
        ResetNeetCalcTable(parseInt(elementID) + 1);
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
        document.removeEventListener('mousemove', mouseMoveHandler);
    };

    resizer.addEventListener('mousedown', mouseDownHandler);
};

var lefts = [];

const ResetNeetCalcTable = (index) => {
    const table = document.getElementById('resizeMe');
    const headers = table.querySelectorAll("th");

    for (let i = index; i < headers.length; i++) {
        const prevLeft = parseInt(window.getComputedStyle(headers[i - 1]).left, 10) || 0;
        lefts.push(prevLeft);
        headers[i - 1].style.left = "auto";

        table.querySelectorAll(`td:nth-child(${i})`).forEach(td => {
            td.style.left = "auto";
        });
    }
};

const SetNewNTHChild = (index, plusLeft) => {
    const table = document.getElementById('resizeMe');
    const headers = table.querySelectorAll("th");
    for (let i = index; i < headers.length; i++) {
        let left = lefts[i - index] || 0;
        if (left > 0) {
            left += plusLeft;
            headers[i - 1].style.left = `${left}px`;

            table.querySelectorAll(`td:nth-child(${i})`).forEach(td => {
                td.style.left = `${left}px`;
            });
        }
    }
    lefts = [];
};

function SaveTemplateJs(event) {
    if (!window.nelCalcDotNetRef) {
        console.warn("nelCalcDotNetRef is not set");
        return;
    }

    window.nelCalcDotNetRef.invokeMethodAsync('SaveTemplateBlazor', event);
}