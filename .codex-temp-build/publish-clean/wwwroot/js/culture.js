window.blazorCulture = {
    get: () => localStorage['BlazorCulture'],
    set: (value) => localStorage['BlazorCulture'] = value
};

window.handleDragOver = function (event) {
    event.preventDefault();
};

// وظيفة لإضافة أحداث السحب لعناصر حسب ID
function setupDragEvents(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;

    el.addEventListener("dragover", e => e.preventDefault());

    el.addEventListener("dragstart", e => {
        e.dataTransfer.setData("0", e.target.id);
    });
}

// العناصر المطلوبة
const dragTargets = [
    "folderTable",         // FoldersTree.razor
    "dropdownResUI",       // dropdownResUI
    "projectCalcUi",        // ProjectsCalculationList.razor
    "projectui"            // ProjectUI.razor
];

// تطبيق الوظيفة على جميع العناصر
dragTargets.forEach(setupDragEvents);