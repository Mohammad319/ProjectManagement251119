// BlazorMHD.UI JavaScript.
//
// This file is an ES module: components import it on demand via
// IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorMHD.UI/js/BlazorMHDUI.js"),
// so consumers no longer need to add a <script> tag manually.
//
// It also assigns the same API to window.BlazorMHDUI (and the legacy
// window.ContextMenuMHD alias) for code that calls the global directly. Because this
// file is an ES module, load it with <script type="module"> (not a classic
// <script src="...">, which cannot parse `export`) if you need that global.

function clamp(v, min, max) {
    return Math.max(min, Math.min(max, v));
}

function ensureRelative(el) {
    if (getComputedStyle(el).position === 'static') {
        el.style.position = 'relative';
    }
}

// ── Resizable splitter drag ──────────────────────────────────────────
export const resizableDiv = {
    getWidth(el) {
        return el ? (el.clientWidth || el.getBoundingClientRect().width || 0) : 0;
    },
    getHeight(el) {
        return el ? (el.clientHeight || el.getBoundingClientRect().height || 0) : 0;
    },
    startVerticalGhostDrag(containerEl, splitterEl, dotNetHelper, opts) {
        const { startY, initialTop, minTop, maxTop, splitterHeight, live } = opts;
        let currentY = startY;
        let rafId = null;

        const ghost = document.createElement('div');
        ghost.style.position = 'absolute';
        ghost.style.left = '0';
        ghost.style.right = '0';
        ghost.style.height = splitterHeight + 'px';
        ghost.style.willChange = 'transform';
        ghost.style.pointerEvents = 'none';
        ghost.style.background = 'transparent';
        ghost.style.borderTop = '1px dashed var(--ghost-color, rgba(120,120,120,.6))';
        ghost.style.borderBottom = '1px dashed var(--ghost-color, rgba(120,120,120,.6))';
        if (live) {
            ghost.style.opacity = '0';
        }

        const crect = containerEl.getBoundingClientRect();
        const srect = splitterEl.getBoundingClientRect();
        const startTopInContainer = srect.top - crect.top;
        ghost.style.transform = `translateY(${startTopInContainer}px)`;
        ensureRelative(containerEl);
        containerEl.appendChild(ghost);

        let lastValue = initialTop;

        function frame() {
            rafId = null;
            const dy = currentY - startY;
            const v = clamp(initialTop + dy, minTop, maxTop);
            lastValue = v;
            if (live) {
                containerEl.style.setProperty('--top-h', v + 'px');
            } else {
                ghost.style.transform = `translateY(${startTopInContainer + dy}px)`;
            }
        }

        function onMove(e) {
            e.preventDefault();
            currentY = e.clientY;
            if (rafId == null) {
                rafId = requestAnimationFrame(frame);
            }
        }

        function onUp(e) {
            e.preventDefault();
            window.removeEventListener('mousemove', onMove, { capture: false });
            window.removeEventListener('mouseup', onUp, { capture: false });
            if (rafId != null) {
                cancelAnimationFrame(rafId);
            }

            containerEl.style.setProperty('--top-h', lastValue + 'px');
            dotNetHelper.invokeMethodAsync('OnVerticalDragEnd', lastValue).finally(() => ghost.remove());
        }

        window.addEventListener('mousemove', onMove, { passive: false });
        window.addEventListener('mouseup', onUp, { passive: false });
    },
    startHorizontalGhostDrag(containerEl, splitterEl, dotNetHelper, opts) {
        const { startX, initialLeft, minLeft, maxLeft, splitterWidth, live } = opts;
        let currentX = startX;
        let rafId = null;

        const ghost = document.createElement('div');
        ghost.style.position = 'absolute';
        ghost.style.top = '0';
        ghost.style.bottom = '0';
        ghost.style.width = splitterWidth + 'px';
        ghost.style.willChange = 'transform';
        ghost.style.pointerEvents = 'none';
        ghost.style.background = 'transparent';
        ghost.style.borderLeft = '1px dashed var(--ghost-color, rgba(120,120,120,.6))';
        ghost.style.borderRight = '1px dashed var(--ghost-color, rgba(120,120,120,.6))';
        if (live) {
            ghost.style.opacity = '0';
        }

        const crect = containerEl.getBoundingClientRect();
        const srect = splitterEl.getBoundingClientRect();
        const startLeftInContainer = srect.left - crect.left;
        ghost.style.transform = `translateX(${startLeftInContainer}px)`;
        ensureRelative(containerEl);
        containerEl.appendChild(ghost);

        let lastValue = initialLeft;

        function frame() {
            rafId = null;
            const dx = currentX - startX;
            const v = clamp(initialLeft + dx, minLeft, maxLeft);
            lastValue = v;
            if (live) {
                containerEl.style.setProperty('--left-w', v + 'px');
            } else {
                ghost.style.transform = `translateX(${startLeftInContainer + dx}px)`;
            }
        }

        function onMove(e) {
            e.preventDefault();
            currentX = e.clientX;
            if (rafId == null) {
                rafId = requestAnimationFrame(frame);
            }
        }

        function onUp(e) {
            e.preventDefault();
            window.removeEventListener('mousemove', onMove, { capture: false });
            window.removeEventListener('mouseup', onUp, { capture: false });
            if (rafId != null) {
                cancelAnimationFrame(rafId);
            }

            containerEl.style.setProperty('--left-w', lastValue + 'px');
            dotNetHelper.invokeMethodAsync('OnHorizontalDragEnd', lastValue).finally(() => ghost.remove());
        }

        window.addEventListener('mousemove', onMove, { passive: false });
        window.addEventListener('mouseup', onUp, { passive: false });
    }
};

// ── Draggable dialog ─────────────────────────────────────────────────
export const dialogDrag = (() => {
    let cleanupActiveDrag = null;

    function cancel() {
        if (cleanupActiveDrag) {
            cleanupActiveDrag();
            cleanupActiveDrag = null;
        }
    }

    function safeInvoke(dotNetHelper, methodName, ...args) {
        return dotNetHelper.invokeMethodAsync(methodName, ...args).catch(() => { });
    }

    function clampPosition(left, top, limits) {
        return {
            left: clamp(left, limits.minLeft, limits.maxLeft),
            top: clamp(top, limits.minTop, limits.maxTop)
        };
    }

    function getDragLimits(containerEl, dialogEl) {
        return {
            minLeft: 0,
            minTop: 0,
            maxLeft: Math.max(0, containerEl.clientWidth - dialogEl.offsetWidth),
            maxTop: Math.max(0, containerEl.clientHeight - dialogEl.offsetHeight)
        };
    }

    function startDrag(dotNetHelper, dialogEl, startX, startY) {
        cancel();

        if (!dialogEl) {
            return;
        }

        const containerEl = dialogEl.parentElement;
        if (!containerEl) {
            return;
        }

        const dialogRect = dialogEl.getBoundingClientRect();
        const containerRect = containerEl.getBoundingClientRect();
        const startLeft = dialogRect.left - containerRect.left;
        const startTop = dialogRect.top - containerRect.top;
        const limits = getDragLimits(containerEl, dialogEl);

        let currentX = startX;
        let currentY = startY;
        let currentLeft = startLeft;
        let currentTop = startTop;
        let rafId = null;

        const previousUserSelect = document.body.style.userSelect;
        const previousCursor = document.body.style.cursor;
        const previousWillChange = dialogEl.style.willChange;

        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'move';
        dialogEl.style.willChange = 'left, top';
        dialogEl.style.left = `${startLeft}px`;
        dialogEl.style.top = `${startTop}px`;
        dialogEl.style.transform = 'translate3d(0, 0, 0)';
        void safeInvoke(dotNetHelper, 'OnDialogDragStart', startLeft, startTop);

        function applyTransform() {
            rafId = null;
            const nextLeft = startLeft + (currentX - startX);
            const nextTop = startTop + (currentY - startY);
            const clamped = clampPosition(nextLeft, nextTop, limits);
            currentLeft = clamped.left;
            currentTop = clamped.top;
            dialogEl.style.left = `${currentLeft}px`;
            dialogEl.style.top = `${currentTop}px`;
        }

        function onMove(e) {
            e.preventDefault();
            currentX = e.clientX;
            currentY = e.clientY;

            if (rafId == null) {
                rafId = requestAnimationFrame(applyTransform);
            }
        }

        function tearDown() {
            window.removeEventListener('mousemove', onMove, { capture: false });
            window.removeEventListener('mouseup', onUp, { capture: false });
            window.removeEventListener('blur', onBlur);
            document.removeEventListener('selectstart', onSelectStart);

            if (rafId != null) {
                cancelAnimationFrame(rafId);
                rafId = null;
            }

            document.body.style.userSelect = previousUserSelect;
            document.body.style.cursor = previousCursor;
            dialogEl.style.willChange = previousWillChange;

            cleanupActiveDrag = null;
        }

        function onUp(e) {
            e.preventDefault();

            if (rafId != null) {
                cancelAnimationFrame(rafId);
            }

            applyTransform();
            tearDown();
            void safeInvoke(dotNetHelper, 'OnDialogDragEnd', currentLeft, currentTop);
        }

        function onBlur() {
            if (rafId != null) {
                cancelAnimationFrame(rafId);
            }

            applyTransform();
            tearDown();
            void safeInvoke(dotNetHelper, 'OnDialogDragEnd', currentLeft, currentTop);
        }

        function onSelectStart(e) {
            e.preventDefault();
        }

        cleanupActiveDrag = tearDown;

        window.addEventListener('mousemove', onMove, { passive: false });
        window.addEventListener('mouseup', onUp, { passive: false });
        window.addEventListener('blur', onBlur);
        document.addEventListener('selectstart', onSelectStart);
    }

    return {
        startDrag,
        cancel
    };
})();

// ── Dropdown panel positioning ───────────────────────────────────────
export const dropdownPanel = {
    // Returns { alignRight, top, x }
    getPosition: function (summaryEl, panelWidthPx) {
        if (!summaryEl) return { alignRight: true, top: 0, x: 0 };
        const rect = summaryEl.getBoundingClientRect();
        const vw = window.innerWidth || document.documentElement.clientWidth;
        const alignRight = rect.right >= panelWidthPx;
        return {
            alignRight: alignRight,
            top: rect.bottom + 6,
            x: alignRight ? (vw - rect.right) : rect.left
        };
    },
};

// ── ContextMenu (merged from ContextMenuMHD) ─────────────────────────
export const contextMenu = (function () {
    const api = {};
    let lastMousePosition = { X: 0, Y: 0 };
    const hideHandlers = new Map();

    api.attachHide = function (menuId) {
        const menu = document.getElementById(menuId);
        if (!menu) return;

        api.disposeHide(menuId);

        const hideFunc = function (event) {
            if (!menu.contains(event.target)) {
                menu.style.display = "none";
                api.disposeHide(menuId);
            }
        };

        const escapeFunc = function (event) {
            if (event.key === "Escape") {
                menu.style.display = "none";
                api.disposeHide(menuId);
            }
        };

        hideHandlers.set(menuId, { hideFunc, escapeFunc });

        setTimeout(() => {
            document.addEventListener("click", hideFunc);
            document.addEventListener("keydown", escapeFunc);
        }, 100);
    };

    api.disposeHide = function (menuId) {
        const handlers = hideHandlers.get(menuId);
        if (!handlers) return;

        document.removeEventListener("click", handlers.hideFunc);
        document.removeEventListener("keydown", handlers.escapeFunc);
        hideHandlers.delete(menuId);
    };

    api.getAdjustedPosition = function (x, y, menuId) {
        const menu = document.getElementById(menuId);
        if (!menu) return { X: x, Y: y };

        const prevDisplay = menu.style.display;
        const prevVisibility = menu.style.visibility;

        menu.style.visibility = "hidden";
        menu.style.display = "block";

        const menuWidth = menu.offsetWidth;
        const menuHeight = menu.offsetHeight;
        const screenWidth = window.innerWidth;
        const screenHeight = window.innerHeight;

        menu.style.display = prevDisplay;
        menu.style.visibility = prevVisibility;

        if (x + menuWidth > screenWidth) x = screenWidth - menuWidth - 5;
        if (y + menuHeight > screenHeight) y = screenHeight - menuHeight - 5;

        return { X: x, Y: y };
    };

    api.getLastMousePosition = function () {
        return lastMousePosition;
    };

    // Combines "read last mouse position" + "clamp to viewport" in a single
    // interop call (one round-trip on Blazor Server instead of two).
    api.showAt = function (menuId) {
        return api.getAdjustedPosition(lastMousePosition.X, lastMousePosition.Y, menuId);
    };

    document.addEventListener("mousemove", function (e) {
        lastMousePosition = { X: e.clientX, Y: e.clientY };
    });

    return api;
})();

// ── Dialog focus trap (keyboard a11y) ────────────────────────────────
export const dialogFocus = (function () {
    const stack = [];
    const SELECTOR =
        'a[href],area[href],button:not([disabled]),input:not([disabled]),' +
        'select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';

    function focusables(el) {
        return Array.prototype.slice
            .call(el.querySelectorAll(SELECTOR))
            .filter(e => e.offsetParent !== null);
    }

    return {
        // Confines Tab/Shift+Tab to the element and moves focus inside it.
        trap: function (element) {
            if (!element) return;

            const previous = document.activeElement;
            const handler = function (e) {
                if (e.key !== "Tab") return;
                const items = focusables(element);
                if (items.length === 0) { e.preventDefault(); element.focus(); return; }

                const first = items[0];
                const last = items[items.length - 1];

                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            };

            document.addEventListener("keydown", handler, true);
            stack.push({ element, handler, previous });

            const items = focusables(element);
            (items[0] || element).focus();
        },

        // Releases the most recent trap and restores the previously focused element.
        release: function () {
            const top = stack.pop();
            if (!top) return;

            document.removeEventListener("keydown", top.handler, true);
            if (top.previous && typeof top.previous.focus === "function") {
                try { top.previous.focus(); } catch (e) { /* element gone */ }
            }
        }
    };
})();

// ── File download (used by MhdTable CSV export) ───────────────────────
// Triggers a client-side download of a text payload as a file.
export function download(fileName, content, mimeType) {
    const blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName || "download.txt";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// ── Focus management (roving-tabindex keyboard navigation) ───────────
// Moves DOM focus to the element with the given id, so arrow-key navigation in
// Tabs/TreeView keeps focus in sync with the active/roving item.
export function focusById(id) {
    if (!id) return;
    const el = document.getElementById(id);
    if (el) el.focus();
}

// ── Backward-compatible globals ──────────────────────────────────────
// Keep the legacy window.BlazorMHDUI / window.ContextMenuMHD surface so apps
// that still load this file via <script src="..."> continue to work.
if (typeof window !== 'undefined') {
    const g = window.BlazorMHDUI = window.BlazorMHDUI || {};
    g.resizableDiv = resizableDiv;
    g.dialogDrag = dialogDrag;
    g.dropdownPanel = dropdownPanel;
    g.contextMenu = contextMenu;
    g.dialogFocus = dialogFocus;
    g.download = download;
    g.focusById = focusById;
    window.ContextMenuMHD = contextMenu;
}
