(function () {
    let tooltip;
    let showTimer;

    function ensureTooltip() {
        if (tooltip?.isConnected) {
            return tooltip;
        }

        tooltip = document.createElement("div");
        tooltip.className = "pm-hover-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.hidden = true;
        document.body.appendChild(tooltip);
        return tooltip;
    }

    function clearTimer() {
        if (showTimer) {
            clearTimeout(showTimer);
            showTimer = undefined;
        }
    }

    function dismiss() {
        clearTimer();
        if (!tooltip) {
            return;
        }

        tooltip.hidden = true;
        tooltip.textContent = "";
    }

    // Renders the tooltip anchored below (or above) the supplied viewport rect.
    function render(text, rect) {
        const box = ensureTooltip();
        box.textContent = text;
        box.hidden = false;
        box.style.visibility = "hidden";

        const boxRect = box.getBoundingClientRect();
        const gap = 6;
        const edge = 8;
        const viewportWidth = document.documentElement.clientWidth || window.innerWidth;
        const viewportHeight = document.documentElement.clientHeight || window.innerHeight;

        let left = rect.left + (rect.width - boxRect.width) / 2;
        left = Math.min(Math.max(edge, left), Math.max(edge, viewportWidth - boxRect.width - edge));

        let top = rect.bottom + gap;
        if (top + boxRect.height > viewportHeight - edge) {
            top = Math.max(edge, rect.top - boxRect.height - gap);
        }

        box.style.left = `${Math.round(left)}px`;
        box.style.top = `${Math.round(top)}px`;
        box.style.visibility = "visible";
    }

    // Anchor-based tooltip. An optional delay (ms) defers the appearance so it does
    // not flash up immediately on hover.
    function show(anchor, text, delay) {
        dismiss();
        if (!anchor || !text) {
            return;
        }

        const run = () => render(text, anchor.getBoundingClientRect());
        if (delay && delay > 0) {
            showTimer = setTimeout(run, delay);
        } else {
            run();
        }
    }

    // Anchor-based tooltip that only appears when the content is actually clipped. Measures the
    // real truncating element (a descendant .truncate, else the anchor) and compares scrollWidth
    // to clientWidth, so a fully visible label shows no tooltip.
    function showIfTruncated(anchor, text) {
        dismiss();
        if (!anchor || !text) {
            return;
        }

        const measured = anchor.querySelector(".truncate") || anchor;
        // +1 guards against sub-pixel rounding that would otherwise show a tooltip for fitting text.
        if (measured.scrollWidth - measured.clientWidth > 1) {
            render(text, anchor.getBoundingClientRect());
        }
    }

    // Cursor-based tooltip used where wrapping each element in an anchor is awkward
    // (e.g. buttons inside a segmented control). Always uses a small delay.
    function showAt(x, y, text, delay) {
        dismiss();
        if (!text) {
            return;
        }

        const rect = { left: x, top: y, bottom: y, width: 0, height: 0 };
        const wait = typeof delay === "number" ? delay : 600;
        if (wait > 0) {
            showTimer = setTimeout(() => render(text, rect), wait);
        } else {
            render(text, rect);
        }
    }

    document.addEventListener("pointerdown", dismiss, true);
    document.addEventListener("scroll", dismiss, true);
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            dismiss();
        }
    }, true);
    window.addEventListener("resize", dismiss);
    window.addEventListener("blur", dismiss);

    window.hoverTooltip = { show, showIfTruncated, showAt, dismiss };
})();
