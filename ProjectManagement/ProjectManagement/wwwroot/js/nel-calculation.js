// wwwroot/js/nel-calculation.js
(function () {
    window.nelCalcDotNetRef = null;

    window.initializeResizableColumns = function (dotNetRef) {
        window.nelCalcDotNetRef = dotNetRef;

        const table = document.getElementById("resizeMe");
        if (!table) return;

        ensureColGroup(table);
        wireResizers(table);
    };

    function ensureColGroup(table) {
        const ths = table.querySelectorAll("thead th");
        if (!ths.length) return;

        let colgroup = table.querySelector("colgroup");
        if (!colgroup) {
            colgroup = document.createElement("colgroup");
            table.insertBefore(colgroup, table.firstChild);
        }

        const need = ths.length;
        let cols = colgroup.querySelectorAll("col");

        if (cols.length < need) {
            for (let i = cols.length; i < need; i++) colgroup.appendChild(document.createElement("col"));
        } else if (cols.length > need) {
            for (let i = cols.length - 1; i >= need; i--) cols[i].remove();
        }

        cols = colgroup.querySelectorAll("col");
        for (let i = 0; i < ths.length; i++) {
            const w = Math.max(30, Math.round(ths[i].getBoundingClientRect().width));
            cols[i].style.width = `${w}px`;
        }
    }

    function wireResizers(table) {
        const ths = table.querySelectorAll("thead th");
        if (!ths.length) return;

        ths.forEach((th) => {
            // اربط مرة واحدة لكل th
            if (th.dataset.resizeWired === "1") return;
            th.dataset.resizeWired = "1";

            if (!th.style.position) th.style.position = "relative";

            // استخدم الموجود أو أنشئ
            let resizer = th.querySelector(":scope > .resizer");
            if (!resizer) {
                resizer = document.createElement("span");
                resizer.className = "resizer";
                th.appendChild(resizer);
            }

            // ضمان أنه يستقبل أحداث
            resizer.style.position = "absolute";
            resizer.style.top = "0";
            resizer.style.right = "0";
            resizer.style.width = "12px";
            resizer.style.height = "100%";
            resizer.style.cursor = "col-resize";
            resizer.style.zIndex = "99999";
            resizer.style.pointerEvents = "auto";
            resizer.style.touchAction = "none";
            resizer.style.background = "transparent";

            attach(table, th, resizer);
        });
    }

    function attach(table, th, resizer) {
        let startX = 0;
        let startW = 0;
        let currentW = 0;
        let raf = 0;

        const getElementId = () => (th.id || "").replace("h", "");

        const onDown = (e) => {
            // ماوس: زر يسار فقط
            if (e.pointerType === "mouse" && e.button !== 0) return;

            const elementID = getElementId();
            if (!elementID) return;

            startX = e.clientX;
            startW = Math.max(30, Math.round(th.getBoundingClientRect().width));
            currentW = startW;

            const onMove = (ev) => {
                const dx = ev.clientX - startX;
                currentW = clamp(startW + dx, 30, 2000);

                cancelAnimationFrame(raf);
                raf = requestAnimationFrame(() => {
                    setColWidth(table, elementID, currentW);
                });
            };

            const onUp = () => {
                window.removeEventListener("pointermove", onMove);
                const w = Math.round(currentW || startW);

                // حفظ مرة واحدة
                if (window.nelCalcDotNetRef) {
                    window.nelCalcDotNetRef.invokeMethodAsync("SaveTemplateBlazor", `${elementID}||${w}`);
                }
            };

            window.addEventListener("pointermove", onMove);
            window.addEventListener("pointerup", onUp, { once: true });

            e.preventDefault();
            e.stopPropagation();
        };

        // Pointer events (أفضل)
        resizer.addEventListener("pointerdown", onDown);
    }

    function setColWidth(table, elementID, widthPx) {
        const n = parseInt(elementID, 10);
        if (!Number.isFinite(n) || n <= 0) return;

        const idx = n - 1; // h1 => col[0]
        const cols = table.querySelectorAll("colgroup col");
        if (!cols.length || idx >= cols.length) return;

        cols[idx].style.width = `${Math.round(widthPx)}px`;
    }

    function clamp(v, min, max) {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }
})();
