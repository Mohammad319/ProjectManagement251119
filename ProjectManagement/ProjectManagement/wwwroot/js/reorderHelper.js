// wwwroot/js/reorderHelper.js

// فعّل/عطّل اللوج من هنا
const DND_LOG = true;

window.reorderHelper = {
    init: function (containerId, dotNetRef) {
        const container = document.getElementById(containerId);
        if (!container) return;

        let dragSrcRow = null;

        function setupRow(row) {
            // لا نربط نفس الصف أكثر من مرة
            if (row.dataset.dndBound === 'true') {
                return;
            }
            row.dataset.dndBound = 'true';

            // نخلي الصف قابل للسحب
            row.draggable = true;

            row.addEventListener('dragstart', function (e) {
                dragSrcRow = row;
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/plain', row.dataset.item || "");
                row.classList.add('opacity-40');
            });

            row.addEventListener('dragend', function () {
                row.classList.remove('opacity-40');
                dragSrcRow = null;
            });

            row.addEventListener('dragover', function (e) {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
            });

            row.addEventListener('drop', function (e) {
                e.preventDefault();

                const targetRow = e.currentTarget;
                if (!dragSrcRow || dragSrcRow === targetRow)
                    return;

                const rows = Array.from(container.children);

                const srcIndex = rows.indexOf(dragSrcRow);
                const targetIndex = rows.indexOf(targetRow);

                if (srcIndex === -1 || targetIndex === -1)
                    return;

                // الترتيب الحالي (قبل الإسقاط) بحسب data-item (رقم العمود)
                const order = rows.map(r => parseInt(r.dataset.item));

                if (DND_LOG) {
                    console.log("Before drop:", order);
                    console.log("srcIndex:", srcIndex, "targetIndex:", targetIndex);
                }

                // نحسب الترتيب الجديد في Array فقط (لا نلمس DOM)
                const moved = order[srcIndex];
                order.splice(srcIndex, 1);            // إزالة العنصر من مكانه القديم
                order.splice(targetIndex, 0, moved);  // إدخاله في المكان الجديد

                if (DND_LOG) {
                    console.log("After drop:", order);
                }

                // إرسال الترتيب الجديد إلى Blazor
                dotNetRef.invokeMethodAsync('UpdateOrder', order);
            });
        }

        // نربط الأحداث لكل الصفوف الحالية
        Array.from(container.children).forEach(setupRow);
    }
};

// Forces the browser to show a "move" cursor (not the "copy"/plus indicator) while dragging
// rows handled by Blazor's own @ondragstart/@ondrop. Blazor's DragEventArgs is a one-way copy,
// so the real dataTransfer.effectAllowed/dropEffect must be set from native listeners. A single
// delegated pair on the stable table container survives row re-renders. Idempotent per element.
window.dragMoveEffect = {
    attach: function (idOrEl) {
        const el = typeof idOrEl === 'string' ? document.getElementById(idOrEl) : idOrEl;
        if (!el || el.__dragMoveBound) return;
        el.__dragMoveBound = true;

        // Capture so it runs alongside Blazor's bubbling dragstart handler.
        el.addEventListener('dragstart', function (e) {
            if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
        }, true);

        el.addEventListener('dragover', function (e) {
            if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
        });
    }
};

// Keeps column-resize and row drag/reorder as two fully separate interactions.
//
// A column resize starts with mousedown/pointerdown on a `.resizer` handle (the shared
// BlazorMHD.UI table engine). While such a resize gesture is active, any row `dragstart` is
// cancelled before it reaches the native drag machinery OR Blazor's delegated @ondragstart
// handler — so resizing a column (even dragging far left to the minimum width and releasing over
// a row) can never start a reorder, never shows a drop line, and never changes the row order.
(function () {
    let resizing = false;

    const closest = (e, sel) => !!(e.target && e.target.closest && e.target.closest(sel));
    const isResizerTarget = (e) => closest(e, '.resizer');
    // Only header interactions of the resizable table can ever be swallowed (see the click handler).
    const isHeaderTarget = (e) => closest(e, '#resizeMe thead') || isResizerTarget(e);

    // Self-healing: a pointerdown/mousedown ON a `.resizer` arms the guard; ANY other pointerdown
    // disarms it. So a fresh interaction anywhere (a dialog button, a form field, a row) always
    // clears a stale "resizing" flag before its own click is dispatched — the guard can never get
    // stuck and block clicks elsewhere in the app (e.g. a dialog's Spara/Save button).
    const begin = (e) => { resizing = isResizerTarget(e); };

    // After a real resize, reset on the next tick (not immediately) so the trailing dragstart AND the
    // synthetic `click` the browser dispatches on release are still blocked — those fire right after
    // pointerup/mouseup, before this macrotask runs.
    const end = () => { if (resizing) setTimeout(() => { resizing = false; }, 0); };
    const reset = () => { resizing = false; };

    // Capture phase + document level so this runs before Blazor's delegated handlers and the engine's
    // own listeners; stopImmediatePropagation guarantees the event is fully swallowed while resizing.
    document.addEventListener('pointerdown', begin, true);
    document.addEventListener('mousedown', begin, true);

    // A column resize must never start a row drag (rows only exist inside the table, so this is safe
    // to apply whenever resizing).
    document.addEventListener('dragstart', function (e) {
        if (resizing) { e.preventDefault(); e.stopImmediatePropagation(); }
    }, true);

    // …and the resize-release click must never reach a sortable <th> (which would flip the list out of
    // manual sort, disable reorder and show the "manuell sortering" tooltip). Scope this strictly to
    // the resizable table header so it can NEVER swallow a click on a dialog button, form control or
    // anything outside the table — even if `resizing` were somehow still set.
    document.addEventListener('click', function (e) {
        if (resizing && isHeaderTarget(e)) { e.preventDefault(); e.stopImmediatePropagation(); }
    }, true);

    // Document-level pointerup/mouseup fire even when the button is released OUTSIDE the table
    // (the engine captures the pointer but the event still bubbles to the document). pointercancel/
    // blur cover an interrupted/lost gesture or release outside the browser window.
    document.addEventListener('pointerup', end, true);
    document.addEventListener('mouseup', end, true);
    document.addEventListener('dragend', end, true);
    document.addEventListener('pointercancel', reset, true);
    window.addEventListener('blur', reset, true);

    // Optional read access for diagnostics / future C# guards.
    window.rowReorderGuard = { isResizing: () => resizing };
})();
