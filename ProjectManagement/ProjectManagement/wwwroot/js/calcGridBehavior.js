window.clampCalcGridScroll = function () {
    const table = document.getElementById('resizeMe');
    const primaryContainer = table?.closest('.divNetCalc');
    const containers = [];

    if (primaryContainer) {
        containers.push(primaryContainer);
    }

    document.querySelectorAll('.divNetCalc').forEach(function (el) {
        if (!containers.includes(el)) {
            containers.push(el);
        }
    });

    if (containers.length === 0) return;

    const clampElement = function (el) {
        const maxTop = Math.max(0, el.scrollHeight - el.clientHeight);
        const maxLeft = Math.max(0, el.scrollWidth - el.clientWidth);
        let changed = false;

        if (el.scrollTop > maxTop) {
            el.scrollTop = maxTop;
            changed = true;
        } else if (el.scrollTop < 0) {
            el.scrollTop = 0;
            changed = true;
        }

        if (el.scrollLeft > maxLeft) {
            el.scrollLeft = maxLeft;
            changed = true;
        } else if (el.scrollLeft < 0) {
            el.scrollLeft = 0;
            changed = true;
        }

        if (changed) {
            el.dispatchEvent(new Event('scroll', { bubbles: true }));
        }
    };

    const clamp = function () {
        containers.forEach(clampElement);
    };

    const clampForFrames = function (remainingFrames) {
        clamp();
        if (remainingFrames <= 0) return;

        window.requestAnimationFrame(function () {
            clampForFrames(remainingFrames - 1);
        });
    };

    clampForFrames(3);
    window.setTimeout(clamp, 50);
    window.setTimeout(clamp, 150);
};

function preventSelectAll(table) {
    if (!table || table._pmSelectAllBlocked) return;
    table._pmSelectAllBlocked = true;
    table.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'a') {
            e.preventDefault();
        }
    });
}

function preventHorizontalBackNavigation(el) {
    if (!el || el._pmWheelBlocked) return;
    el._pmWheelBlocked = true;
    el.addEventListener('wheel', (e) => {
        if (e.deltaX === 0) return;
        const atLeft = el.scrollLeft <= 0 && e.deltaX < 0;
        const atRight = el.scrollLeft >= el.scrollWidth - el.clientWidth && e.deltaX > 0;
        if (atLeft || atRight) e.preventDefault();
    }, { passive: false });
}
