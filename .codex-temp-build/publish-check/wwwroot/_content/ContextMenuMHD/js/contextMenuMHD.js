(function () {
    const api = window.ContextMenuMHD || {};
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

    document.addEventListener("mousemove", function (e) {
        lastMousePosition = { X: e.clientX, Y: e.clientY };
    });

    window.ContextMenuMHD = api;
})();
