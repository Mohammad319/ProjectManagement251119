(function (window) {
    const root = window.BlazorMHDUI = window.BlazorMHDUI || {};
    const { clamp, ensureRelative } = root._internal;

    const resizableDiv = {
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

    root.resizableDiv = resizableDiv;
    window.resizableDiv = resizableDiv;
})(window);
