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

const PM_COLUMN_BOUNDS = {
    rowNumber: [45, 55],
    code: [55, 160],
    name: [150, 520],
    project: [150, 520],
    status: [100, 260],
    calculationType: [100, 260],
    projectType: [100, 260],
    access: [120, 260],
    shared: [120, 260],
    archived: [80, 220],
    deadline: [110, 180],
    createdAt: [110, 180],
    updatedAt: [110, 180],
    publicationDate: [110, 180],
    decisionDate: [110, 180],
    start: [110, 180],
    end: [110, 180],
    qa: [110, 180],
    department: [90, 260],
    folder: [90, 260],
    responsible: [100, 300],
    inspector: [100, 300],
    organisation: [120, 360],
    procurementNumber: [120, 360],
    customerReference: [120, 360],
    procurementName: [120, 360],
    procurementMethods: [110, 360],
    contract: [90, 260],
    compensation: [110, 260],
    procurementProcedure: [130, 360],
    byggherre: [120, 360],
    clientsManager: [120, 360],
    designer: [120, 360],
    address: [140, 360],
    supervisor: [120, 360],
    version: [70, 160],
    calculationRole: [130, 260],
    priority: [80, 220],
    timeMonth: [80, 220],
    tax: [60, 140],
    privacy: [60, 140],
};

function clampWidth(columnKey, width) {
    const bounds = PM_COLUMN_BOUNDS[columnKey] || [80, 360];
    return Math.min(bounds[1], Math.max(bounds[0], Number(width) || bounds[0]));
}

function clampWidths(widths) {
    return Object.fromEntries(
        Object.entries(widths)
            .filter(([, width]) => Number(width) > 0)
            .map(([columnKey, width]) => [columnKey, clampWidth(columnKey, width)])
    );
}

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
    tableColumns.applySavedWidths(clampWidths(widths), document.getElementById('resizeMe'));
};
