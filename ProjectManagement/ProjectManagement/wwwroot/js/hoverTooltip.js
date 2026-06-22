(function () {
    let tooltip;

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

    function dismiss() {
        if (!tooltip) {
            return;
        }

        tooltip.hidden = true;
        tooltip.textContent = "";
    }

    function show(anchor, text) {
        dismiss();
        if (!anchor || !text) {
            return;
        }

        const box = ensureTooltip();
        box.textContent = text;
        box.hidden = false;
        box.style.visibility = "hidden";

        const anchorRect = anchor.getBoundingClientRect();
        const boxRect = box.getBoundingClientRect();
        const gap = 6;
        const edge = 8;
        const viewportWidth = document.documentElement.clientWidth || window.innerWidth;
        const viewportHeight = document.documentElement.clientHeight || window.innerHeight;

        let left = anchorRect.left + (anchorRect.width - boxRect.width) / 2;
        left = Math.min(Math.max(edge, left), Math.max(edge, viewportWidth - boxRect.width - edge));

        let top = anchorRect.bottom + gap;
        if (top + boxRect.height > viewportHeight - edge) {
            top = Math.max(edge, anchorRect.top - boxRect.height - gap);
        }

        box.style.left = `${Math.round(left)}px`;
        box.style.top = `${Math.round(top)}px`;
        box.style.visibility = "visible";
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

    window.hoverTooltip = { show, dismiss };
})();
