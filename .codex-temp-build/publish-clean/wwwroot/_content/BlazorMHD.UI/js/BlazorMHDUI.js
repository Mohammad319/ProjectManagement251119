(function () {
    function clamp(v, min, max) {
        return Math.max(min, Math.min(max, v));
    }

    function ensureRelative(el) {
        if (getComputedStyle(el).position === 'static') {
            el.style.position = 'relative';
        }
    }

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

    const dialogDrag = (() => {
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

    window.BlazorMHDUI = window.BlazorMHDUI || {};
    window.BlazorMHDUI.resizableDiv = resizableDiv;
    window.BlazorMHDUI.dialogDrag = dialogDrag;
})();
