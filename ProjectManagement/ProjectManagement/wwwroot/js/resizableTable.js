// Thin adapter over BlazorMHD.UI's generic `tableColumns` engine.
//
// The resizable/frozen-column engine now lives in the shared library
// (_content/BlazorMHD.UI/js/BlazorMHDUI.js → export `tableColumns`). This file only:
//   1. resolves the project's #resizeMe table,
//   2. forwards the per-table .NET reference + PM-specific selector conventions, and
//   3. keeps the app-local behaviors (preventSelectAll / preventHorizontalBackNavigation,
//      defined in calcGridBehavior.js) that are not part of the generic engine.
//
// The public contract (window.initializeResizableColumns / window.applySavedColumnWidths
// / the "columnKey||width" payload to SaveTemplateBlazor) is unchanged, so every
// consumer (ProjectsUI, ProjectsCalculationList, CalcDataGrid) keeps working as-is.

// PM table conventions handed to the generic engine.
const PM_TABLE_OPTIONS = {
    scrollContainerSelector: '.divNetCalc',
    filterRowSelector: 'thead tr#headerFilter',
    summaryRowSelector: 'tbody tr.calc-summary-row',
    headerRowSelector: 'thead tr#header-row',
    idPrefix: 'h',
    frozenDataAttr: 'pmFrozen',            // data-pm-frozen
    overflowTitleDataAttr: 'pmOverflowTitle', // data-pm-overflow-title
    saveMethodName: 'SaveTemplateBlazor',
};

// Cache the library module import so init + applySavedColumnWidths share one load.
let _mhdModulePromise = null;
function loadEngine() {
    // Reuse the already-loaded module when present (the common case: MhdJsInterop
    // imports it on first use), so we never spin up a second module instance.
    if (window.BlazorMHDUI && window.BlazorMHDUI.tableColumns) {
        return Promise.resolve(window.BlazorMHDUI.tableColumns);
    }
    if (!_mhdModulePromise) {
        const url = new URL('_content/BlazorMHD.UI/js/BlazorMHDUI.js', document.baseURI).href;
        _mhdModulePromise = import(url).then(m => m.tableColumns);
    }
    return _mhdModulePromise;
}

window.initializeResizableColumns = async function (dotNetRef) {
    const table = document.getElementById('resizeMe');
    if (!table) return;

    const tableColumns = await loadEngine();
    tableColumns.initialize(table, dotNetRef, PM_TABLE_OPTIONS);

    // App-local behaviors that aren't part of the generic engine.
    if (typeof preventSelectAll === 'function') preventSelectAll(table);

    const scrollContainer = table.closest(PM_TABLE_OPTIONS.scrollContainerSelector);
    if (scrollContainer && typeof preventHorizontalBackNavigation === 'function') {
        preventHorizontalBackNavigation(scrollContainer);
    }
};

// Seed server-restored widths (keyed by column key, e.g. "code") and apply them.
window.applySavedColumnWidths = async function (widths) {
    if (!widths) return;
    const tableColumns = await loadEngine();
    tableColumns.applySavedWidths(widths, document.getElementById('resizeMe'));
};
