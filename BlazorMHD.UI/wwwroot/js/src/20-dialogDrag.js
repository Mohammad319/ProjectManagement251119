(function (window) {
    const root = window.BlazorMHDUI = window.BlazorMHDUI || {};
    const { clamp } = root._internal;

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

            function applyPosition() {
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
                    rafId = requestAnimationFrame(applyPosition);
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

                applyPosition();
                tearDown();
                void safeInvoke(dotNetHelper, 'OnDialogDragEnd', currentLeft, currentTop);
            }

            function onBlur() {
                if (rafId != null) {
                    cancelAnimationFrame(rafId);
                }

                applyPosition();
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

    root.dialogDrag = dialogDrag;
})(window);
