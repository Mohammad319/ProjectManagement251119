window.nelCalcDotNetRef = window.nelCalcDotNetRef || null;

window.initializeResizableColumns = function (dotNetRef) {
    if (dotNetRef) {
        window.nelCalcDotNetRef = dotNetRef;
    }

    const table = document.getElementById("resizeMe");
    if (!table) return;

    createResizableTable(table);
};

const MIN_COLUMN_WIDTH = 30;
let lefts = [];

const createResizableTable = (table) => {
    const cols = table.querySelectorAll("th");

    cols.forEach((col) => {
        if (!col.style.position) {
            col.style.position = "relative";
        }

        const allResizers = Array.from(col.children).filter((el) =>
            el.classList && el.classList.contains("resizer")
        );

        let resizer = allResizers[0];
        for (let i = 1; i < allResizers.length; i++) {
            allResizers[i].remove();
        }

        if (!resizer) {
            resizer = document.createElement("div");
            resizer.classList.add("resizer");
            col.appendChild(resizer);
        }

        resizer.style.height = `${table.offsetHeight}px`;

        if (resizer.dataset.bound !== "1") {
            createResizableColumn(col, resizer);
            resizer.dataset.bound = "1";
        }
    });
};

const createResizableColumn = function (col, resizer) {
    let x = 0;
    let w = 0;
    let elementID = "";

    const mouseMoveHandler = function (e) {
        const nextWidth = Math.max(MIN_COLUMN_WIDTH, w + e.clientX - x);
        col.style.width = `${nextWidth}px`;
    };

    const mouseUpHandler = () => {
        resizer.classList.remove("resizing");

        const parsedWidth = parseInt(window.getComputedStyle(col).width, 10);
        const newWidth = Number.isFinite(parsedWidth)
            ? Math.max(MIN_COLUMN_WIDTH, parsedWidth)
            : w;

        SaveTemplateJs(`${elementID}||${newWidth}`);
        SetNewNTHChild(+elementID + 1, newWidth - w);

        document.removeEventListener("mousemove", mouseMoveHandler);
    };

    const mouseDownHandler = function (e) {
        if (e.button !== 0) return;

        elementID = (col.id || "").replace("h", "");
        if (!elementID) return;

        lefts = [];
        ResetNeetCalcTable(parseInt(elementID, 10) + 1);

        x = e.clientX;

        const parsedWidth = parseInt(window.getComputedStyle(col).width, 10);
        w = Number.isFinite(parsedWidth)
            ? Math.max(MIN_COLUMN_WIDTH, parsedWidth)
            : MIN_COLUMN_WIDTH;

        document.addEventListener("mousemove", mouseMoveHandler);
        document.addEventListener("mouseup", mouseUpHandler, { once: true });

        resizer.classList.add("resizing");
        e.preventDefault();
        e.stopPropagation();
    };

    resizer.addEventListener("mousedown", mouseDownHandler);
};

const ResetNeetCalcTable = (index) => {
    const table = document.getElementById("resizeMe");
    if (!table) return;

    const headers = table.querySelectorAll("th");

    for (let i = index; i < headers.length; i++) {
        const prevLeft = parseInt(window.getComputedStyle(headers[i - 1]).left, 10) || 0;
        lefts.push(prevLeft);
        headers[i - 1].style.left = "auto";

        table.querySelectorAll(`td:nth-child(${i})`).forEach((td) => {
            td.style.left = "auto";
        });
    }
};

const SetNewNTHChild = (index, plusLeft) => {
    const table = document.getElementById("resizeMe");
    if (!table) return;

    const headers = table.querySelectorAll("th");
    for (let i = index; i < headers.length; i++) {
        let left = lefts[i - index] || 0;
        if (left > 0) {
            left += plusLeft;
            headers[i - 1].style.left = `${left}px`;

            table.querySelectorAll(`td:nth-child(${i})`).forEach((td) => {
                td.style.left = `${left}px`;
            });
        }
    }

    lefts = [];
};

function SaveTemplateJs(event) {
    if (!window.nelCalcDotNetRef) {
        return;
    }

    window.nelCalcDotNetRef
        .invokeMethodAsync("SaveTemplateBlazor", event)
        .catch(() => { });
}
