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
