window._intersectionObservers = window._intersectionObservers || {};

window.observeIntersection = function (dotNetHelper, elementId, rootId) {
    const prev = window._intersectionObservers[elementId];
    if (prev) { prev.disconnect(); delete window._intersectionObservers[elementId]; }

    const el = document.getElementById(elementId);
    if (!el) return;

    const root = rootId ? document.getElementById(rootId) : null;

    const obs = new IntersectionObserver(entries => {
        if (entries[0].isIntersecting) {
            dotNetHelper.invokeMethodAsync('OnScrolledToBottom');
        }
    }, { root, rootMargin: '120px' });

    obs.observe(el);
    window._intersectionObservers[elementId] = obs;
};

window.unobserveIntersection = function (elementId) {
    const obs = window._intersectionObservers[elementId];
    if (obs) { obs.disconnect(); delete window._intersectionObservers[elementId]; }
};
