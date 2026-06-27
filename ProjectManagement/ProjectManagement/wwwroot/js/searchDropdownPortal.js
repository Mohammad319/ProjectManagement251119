// Teleports a dropdown popup out of an ancestor that clips/offsets it (a draggable
// dialog gets a CSS `transform` => becomes the containing block for `position:fixed`,
// and its surface/body use `overflow:hidden/auto`). Moving the popup host to <body>
// lets the fixed popup anchor to the viewport and never be clipped.
//
// Used by ProjectsSearch's Typ / Matchning / Kalkylstatus dropdowns.
window.searchDropdownPortal = (function () {
    const state = { host: null, panel: null, anchor: null, dotnet: null, gap: 6 };

    function position() {
        const { panel, anchor, gap } = state;
        if (!panel || !anchor || typeof anchor.getBoundingClientRect !== 'function') return;

        const a = anchor.getBoundingClientRect();
        const vw = document.documentElement.clientWidth || window.innerWidth || 0;
        const vh = document.documentElement.clientHeight || window.innerHeight || 0;

        // The popup is at least as wide as the field that opened it.
        if (a.width > 0) panel.style.minWidth = a.width + 'px';

        const pw = panel.offsetWidth || a.width;
        const ph = panel.offsetHeight || 0;

        // Horizontal: left edge aligned with the field, clamped into the viewport.
        let left = a.left;
        const maxLeft = Math.max(gap, vw - pw - gap);
        if (left > maxLeft) left = maxLeft;
        if (left < gap) left = gap;

        // Vertical: directly below the field; flip above when there is not enough room
        // below and there is more room above.
        const below = vh - a.bottom;
        const above = a.top;
        const flip = below < (ph + gap) && above > below;
        const top = flip ? Math.max(gap, a.top - ph - gap) : a.bottom + gap;

        panel.style.left = Math.round(left) + 'px';
        panel.style.top = Math.round(top) + 'px';
        panel.style.visibility = 'visible';
    }

    function onScroll() { position(); }
    function onResize() { position(); }

    function onPointerDown(e) {
        const { panel, dotnet } = state;
        if (!panel || !dotnet) return;
        const t = e.target;
        if (panel.contains(t)) return;                                       // click inside the list — keep open
        if (t && t.closest && t.closest('[data-search-dropdown-trigger]')) return; // a trigger button toggles itself
        dotnet.invokeMethodAsync('OnPortalDismiss');
    }

    function onKeyDown(e) {
        if (e.key === 'Escape' && state.dotnet) state.dotnet.invokeMethodAsync('OnPortalDismiss');
    }

    function detach() {
        window.removeEventListener('scroll', onScroll, true);
        window.removeEventListener('resize', onResize);
        document.removeEventListener('pointerdown', onPointerDown, true);
        document.removeEventListener('keydown', onKeyDown, true);
    }

    return {
        // Move the (always-rendered) host element to <body> so its fixed child popup
        // escapes the dialog's transform/overflow. Safe to call repeatedly.
        mount: function (host) {
            state.host = host;
            if (host && host.parentElement !== document.body) document.body.appendChild(host);
        },
        open: function (panel, anchor, dotnet) {
            detach();
            state.panel = panel;
            state.anchor = anchor;
            state.dotnet = dotnet;
            if (panel) panel.style.visibility = 'hidden';
            position();
            window.addEventListener('scroll', onScroll, true);
            window.addEventListener('resize', onResize);
            document.addEventListener('pointerdown', onPointerDown, true);
            document.addEventListener('keydown', onKeyDown, true);
        },
        reposition: position,
        close: function () {
            detach();
            state.panel = null;
            state.anchor = null;
        },
        dispose: function () {
            detach();
            if (state.host && state.host.parentElement) state.host.parentElement.removeChild(state.host);
            state.host = null;
            state.panel = null;
            state.anchor = null;
            state.dotnet = null;
        }
    };
})();
