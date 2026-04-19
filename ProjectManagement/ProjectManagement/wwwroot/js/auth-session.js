(function () {
    const keepAliveUrl = "/auth/session/keep-alive";
    const accountPrefix = "/Account";
    const retryQueryKey = "authRetry";
    const intervalMs = 5 * 60 * 1000;
    const idleWindowMs = 10 * 60 * 1000;

    let started = false;
    let lastActivityAt = Date.now();

    function markActivity() {
        lastActivityAt = Date.now();
    }

    function isAccountPage() {
        return window.location.pathname.startsWith(accountPrefix);
    }

    function cleanupRetryFlag() {
        const currentUrl = window.location.pathname + window.location.search + window.location.hash;
        const cleanedUrl = removeRetryFlag(currentUrl);

        if (cleanedUrl !== currentUrl) {
            window.history.replaceState(window.history.state, "", cleanedUrl);
        }
    }

    function removeRetryFlag(localUrl) {
        const [pathAndQuery, fragment] = splitFragment(localUrl);
        const queryIndex = pathAndQuery.indexOf("?");
        if (queryIndex < 0) {
            return localUrl;
        }

        const path = pathAndQuery.substring(0, queryIndex);
        const tokens = pathAndQuery
            .substring(queryIndex + 1)
            .split("&")
            .filter(Boolean)
            .filter(token => decodeURIComponent(token.split("=")[0]) !== retryQueryKey);

        if (tokens.length === 0) {
            return path + fragment;
        }

        return path + "?" + tokens.join("&") + fragment;
    }

    function splitFragment(localUrl) {
        const fragmentIndex = localUrl.indexOf("#");
        if (fragmentIndex < 0) {
            return [localUrl, ""];
        }

        return [localUrl.substring(0, fragmentIndex), localUrl.substring(fragmentIndex)];
    }

    async function sendKeepAlive(redirectOnExpiry = false) {
        if (document.hidden || isAccountPage()) {
            return;
        }

        if (Date.now() - lastActivityAt > idleWindowMs) {
            return;
        }

        try {
            const response = await fetch(keepAliveUrl, {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (response.ok) {
                cleanupRetryFlag();
            } else if (response.status === 401 && redirectOnExpiry) {
                const returnUrl = window.location.pathname + window.location.search + window.location.hash;
                window.location.replace("/Account/Login?returnUrl=" + encodeURIComponent(returnUrl));
            }
        }
        catch {
        }
    }

    function attachActivityListeners() {
        const events = ["pointerdown", "keydown", "scroll", "focus", "touchstart"];

        for (const eventName of events) {
            window.addEventListener(eventName, markActivity, { passive: true });
        }

        document.addEventListener("visibilitychange", () => {
            if (!document.hidden) {
                markActivity();
                void sendKeepAlive(true);
            }
        });

        window.addEventListener("online", () => {
            markActivity();
            void sendKeepAlive();
        });
    }

    function start() {
        if (started) {
            return;
        }

        started = true;
        cleanupRetryFlag();
        attachActivityListeners();

        window.setInterval(() => {
            void sendKeepAlive();
        }, intervalMs);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }
})();
