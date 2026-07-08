window._intersectionObservers = window._intersectionObservers || {};

window.observeIntersection = function (dotNetHelper, elementId) {
    const prev = window._intersectionObservers[elementId];
    if (prev) { prev.disconnect(); delete window._intersectionObservers[elementId]; }

    const el = document.getElementById(elementId);
    if (!el) return;

    const obs = new IntersectionObserver(function (entries) {
        if (entries[0].isIntersecting) {
            dotNetHelper.invokeMethodAsync('OnScrolledToBottom').catch(function () { });
        }
    }, { rootMargin: '150px' });

    obs.observe(el);
    window._intersectionObservers[elementId] = obs;
};

window.unobserveIntersection = function (elementId) {
    const obs = window._intersectionObservers[elementId];
    if (obs) { obs.disconnect(); delete window._intersectionObservers[elementId]; }
};

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

window.printCalcGrid = function (title) {
    const table = document.getElementById('resizeMe');
    if (!table) return;

    let cssText = '';
    Array.from(document.styleSheets).forEach(function (sheet) {
        try {
            Array.from(sheet.cssRules).forEach(function (rule) {
                cssText += rule.cssText + '\n';
            });
        } catch (_) { }
    });

    const win = window.open('', '_blank', 'width=1400,height=900');
    if (!win) return;

    // Företagsprofilen (från Företagsinställningar) stämplas in som sidhuvud +
    // diskret "Skapad i ATA COST"-sidfot. pmCompanyBrand definieras i reportExport.js.
    const brand = window.pmCompanyBrand;
    const companyPromise = brand ? brand.get() : Promise.resolve(null);

    companyPromise.then(function (company) {
        const headerHtml = brand ? brand.headerHtml(company) : '';
        const footerHtml = brand ? brand.footerHtml(company) + brand.brandFooterHtml() : '';

        win.document.write('<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + (title || 'Print') + '</title><style>' +
            cssText +
            'body{margin:6mm;font-family:system-ui,-apple-system,sans-serif;font-size:11px;padding-bottom:8mm;}' +
            'table{border-collapse:collapse;width:100%;}' +
            'thead{display:table-header-group;}' +
            'tr{page-break-inside:avoid;}' +
            '</style></head><body>' + headerHtml + table.outerHTML + footerHtml + '</body></html>');
        win.document.close();
        win.focus();
        setTimeout(function () { win.print(); win.close(); }, 400);
    });
};
